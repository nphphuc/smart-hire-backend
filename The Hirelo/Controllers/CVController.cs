using Amazon.RDS.Internal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using The_Hirelo.Common;
using The_Hirelo.Data;
using The_Hirelo.DTOs.Requests;
using The_Hirelo.DTOs.Responses;
using The_Hirelo.Repositories;
using The_Hirelo.Services;

namespace The_Hirelo.Controllers
{
    [ApiController]
    [Route("api/cv")]
    [Authorize]
    public class CVController : ControllerBase
    {
        private readonly ICVService _cvService;
        private readonly ICandidateProfileRepository _profileRepo;
        private readonly IWebSocketManager _wsManager;
        private readonly ILogger<CVController> _logger;

        private static readonly string[] AllowedExtensions = [".pdf", ".doc", ".docx"];
        private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10MB
        private readonly HireloDbContext _context;
        private readonly IConfiguration _configuration;

        public CVController(
            ICVService cvService,
            ICandidateProfileRepository profileRepo,
            IWebSocketManager wsManager,
            ILogger<CVController> logger,
            HireloDbContext context,
            IConfiguration configuration)
        {
            _cvService = cvService;
            _profileRepo = profileRepo;
            _wsManager = wsManager;
            _logger = logger;
            _context = context;
            _configuration = configuration;
        }

        // POST /api/cv/upload
        // Diagram: FE → POST /cv/upload → verify token → S3 → INSERT profile → PUBLISH queue → 202 + profileId
        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadCV([FromForm] CVUploadRequest request)
        {
            var cognitoSub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(cognitoSub))
                return Unauthorized(new { message = "Invalid token" });

            // Resolve userId từ DB thông qua CognitoSub
            var user = await _context.Users.FirstOrDefaultAsync(u => u.CognitoSub == cognitoSub);
            if (user == null)
                return Unauthorized(new { message = "User not found in database." });

            var userId = user.Id;
            // Validate file
            var ext = Path.GetExtension(request.File.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
                return BadRequest(new { message = "Chỉ chấp nhận file PDF, DOC, DOCX." });

            if (request.File.Length > MaxFileSizeBytes)
                return BadRequest(new { message = "File vượt quá giới hạn 10MB." });

            try
            {
                // Step 1: putObject → S3 → fileUrl, fileKey
                var (fileUrl, fileKey) = await _cvService.UploadToS3Async(request.File, userId);

                // Step 2: INSERT candidate_profile (status: PROCESSING) → profileId
                var profile = await _cvService.CreateProfileAsync(userId, request.JobId, fileUrl, fileKey);

                // Step 3: PUBLISH cv_parse_queue(candidateSub, fileKey, jobId)
                await _cvService.PublishCVParseQueueAsync(cognitoSub, fileKey, request.JobId);

                _logger.LogInformation("CV accepted | ProfileId={Id} | Job={JobId}", profile.Id, request.JobId);

                // 202 Accepted + profileId (FE dùng profileId để poll hoặc nhận WS event)
                return Accepted(new CVUploadResponse
                {
                    ProfileId = profile.Id,
                    Status = profile.Status,
                    Message = "CV đã được nhận. Đang phân tích..."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CV upload failed | User={UserId}", userId);
                return StatusCode(500, new { message = $"Upload thất bại: {ex.Message}" });
            }
        }

        // POST /api/cv/presigned-url
        // FE gọi để lấy URL → tự PUT file lên S3
        // S3 Event Notification tự trigger Lambda (không cần FE gọi thêm)
        [HttpPost("presigned-url")]
        public async Task<IActionResult> GetPresignedUrl([FromBody] CVPresignedUrlRequest request)
        {
            var cognitoSub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(cognitoSub))
                return Unauthorized(new { message = "Invalid token" });

            // Resolve userId từ DB — giống hệt endpoint upload cũ
            var user = await _context.Users.FirstOrDefaultAsync(u => u.CognitoSub == cognitoSub);
            if (user == null)
                return Unauthorized(new { message = "User not found in database." });

            // Validate extension
            var ext = Path.GetExtension(request.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
                return BadRequest(new { message = "Chỉ chấp nhận file PDF, DOC, DOCX." });

            if (request.JobId == null || request.JobId == Guid.Empty)
                return BadRequest(new { message = "JobId là bắt buộc." });

            try
            {
                // Tạo fileKey theo đúng format của CVService hiện tại
                var fileKey = $"cvs/{user.Id}/{Guid.NewGuid()}{ext}";

                // Lấy presigned URL từ CVService (thêm method này vào ICVService)
                var presignedUrl = await _cvService.GeneratePresignedUploadUrlAsync(fileKey, request.ContentType);

                // INSERT candidate_profile với status PROCESSING ngay lúc này
                var fileUrl = $"https://{_configuration["AWS:S3:BucketName"]}.s3.ap-southeast-1.amazonaws.com/{fileKey}";
                var profile = await _cvService.CreateProfileAsync(user.Id, request.JobId.Value, fileUrl, fileKey);

                _logger.LogInformation("Presigned URL issued | ProfileId={Id} | Job={JobId}", profile.Id, request.JobId);

                return Ok(new CVPresignedUrlResponse
                {
                    PresignedUrl = presignedUrl,
                    FileKey = fileKey,
                    ProfileId = profile.Id,
                    ExpiresInSeconds = 300
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Presigned URL generation failed | User={UserId}", user.Id);
                return StatusCode(500, new { message = $"Tạo URL thất bại: {ex.Message}" });
            }
        }

        // GET /api/candidates/{profileId}
        // Diagram: FE fetchProfile(profileId) → SELECT * FROM candidate_profiles → full profile data
        [HttpGet("/api/candidates/{profileId:guid}")]
        public async Task<ActionResult<CandidateProfileResponse>> GetProfile(Guid profileId)
        {
            var profile = await _profileRepo.GetByIdAsync(profileId);

            if (profile == null)
                return NotFound(new { message = "Profile không tồn tại." });

            return Ok(new CandidateProfileResponse
            {
                Id = profile.Id,
                UserId = profile.UserId,
                JobId = profile.JobId,
                FileUrl = profile.FileUrl,
                Seniority = profile.Seniority,
                ParsedSkillsJson = profile.ParsedSkillsJson,
                Strengths = profile.Strengths,
                Gaps = profile.Gaps,
                MatchingScore = profile.MatchingScore,
                Status = profile.Status,
                CreatedAt = profile.CreatedAt ?? DateTime.UtcNow,
                UpdatedAt = profile.UpdatedAt
            });
        }

        // GET /api/jobs/{jobId}/candidates
        // Recruiter Dashboard: danh sách ứng viên theo job, sort theo score
        [HttpGet("/api/jobs/{jobId:guid}/candidates")]
        public async Task<ActionResult<List<CandidateProfileResponse>>> GetCandidatesForJob(Guid jobId)
        {
            var profiles = await _profileRepo.GetByJobIdAsync(jobId);

            return Ok(profiles.Select(p => new CandidateProfileResponse
            {
                Id = p.Id,
                UserId = p.UserId,
                JobId = p.JobId,
                Seniority = p.Seniority,
                ParsedSkillsJson = p.ParsedSkillsJson,
                Strengths = p.Strengths,
                Gaps = p.Gaps,
                MatchingScore = p.MatchingScore,
                Status = p.Status,
                CreatedAt = p.CreatedAt ?? DateTime.UtcNow,
                UpdatedAt = p.UpdatedAt
            }));
        }

        // GET /api/cv/ws — WebSocket cho Recruiter Dashboard (event_cv_ready)
        [HttpGet("ws")]
        public async Task WebSocketEndpoint()
        {
            if (!HttpContext.WebSockets.IsWebSocketRequest)
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            var cognitoSub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(cognitoSub))
            {
                HttpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            var socket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            _wsManager.AddSocket(cognitoSub, socket);
            _logger.LogInformation("WebSocket connected: {UserId}", cognitoSub);

            // Giữ kết nối sống cho đến khi client đóng
            var buffer = new byte[1024];
            var result = await socket.ReceiveAsync(buffer, CancellationToken.None);
            while (!result.CloseStatus.HasValue)
                result = await socket.ReceiveAsync(buffer, CancellationToken.None);

            _wsManager.RemoveSocket(cognitoSub);
            await socket.CloseAsync(result.CloseStatus!.Value, result.CloseStatusDescription, CancellationToken.None);
            _logger.LogInformation("WebSocket disconnected: {UserId}", cognitoSub);
        }

        // GET /api/cv/my-profiles
        // Candidate xem tất cả CV mình đã upload
        [HttpGet("my-profiles")]
        public async Task<ActionResult<List<CandidateProfileResponse>>> GetMyProfiles()
        {
            var cognitoSub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.CognitoSub == cognitoSub);
            if (user == null)
                return Unauthorized(new { message = "User not found." });

            var profiles = await _profileRepo.GetByUserIdAsync(user.Id);

            return Ok(profiles.Select(p => new CandidateProfileResponse
            {
                Id = p.Id,
                UserId = p.UserId,
                JobId = p.JobId,
                FileUrl = p.FileUrl,
                Seniority = p.Seniority,
                ParsedSkillsJson = p.ParsedSkillsJson,
                Strengths = p.Strengths,
                Gaps = p.Gaps,
                MatchingScore = p.MatchingScore,
                Status = p.Status,
                CreatedAt = p.CreatedAt ?? DateTime.UtcNow,
                UpdatedAt = p.UpdatedAt
            }));
        }

        // DELETE /api/cv/{profileId}
        // Candidate xóa CV của mình
        [HttpDelete("{profileId:guid}")]
        public async Task<IActionResult> DeleteProfile(Guid profileId)
        {
            var cognitoSub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.CognitoSub == cognitoSub);
            if (user == null)
                return Unauthorized(new { message = "User not found." });

            var profile = await _profileRepo.GetByIdAsync(profileId);
            if (profile == null)
                return NotFound(new { message = "Profile không tồn tại." });

            // Chỉ cho phép xóa CV của chính mình
            if (profile.UserId != user.Id)
                return Forbid();

            await _profileRepo.DeleteAsync(profile);

            _logger.LogInformation("Profile deleted | ProfileId={Id} | UserId={UserId}", profileId, user.Id);

            return NoContent();
        }

    }


}
