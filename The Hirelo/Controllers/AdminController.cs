using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using The_Hirelo.Data;
using The_Hirelo.DTOs.Responses;
using The_Hirelo.Enums;
using The_Hirelo.Services;

namespace The_Hirelo.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly ILogger<AdminController> _logger;
        private readonly HireloDbContext _context;

        public AdminController(IAdminService adminService, ILogger<AdminController> logger, HireloDbContext context)
        {
            _adminService = adminService;
            _logger = logger;
            _context = context;
        }

        // GET /api/admin/users
        // Danh sách tất cả users, filter theo role nếu cần
        [HttpGet("users")]
        public async Task<ActionResult<List<AdminUserResponse>>> GetUsers(
            [FromQuery] string? role = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var query = _context.Users.AsQueryable();

            if (Enum.TryParse<UserRole>(role, true, out var roleEnum))
                query = query.Where(u => u.Role == roleEnum);

            var total = await query.CountAsync();
            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new AdminUserResponse
                {
                    Id = u.Id,
                    Email = u.Email,
                    Role = u.Role.ToString(),
                    CognitoSub = u.CognitoSub,
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                total,
                page,
                pageSize,
                data = users
            });
        }

        // GET /api/admin/candidates
        // Danh sách tất cả candidate profiles, kèm thông tin user và job
        [HttpGet("candidates")]
        public async Task<ActionResult> GetCandidates(
            [FromQuery] string? status = null,
            [FromQuery] Guid? jobId = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var query = _context.CandidateProfiles
                .Include(p => p.User)
                .Include(p => p.Job)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(p => p.Status == status);

            if (jobId.HasValue)
                query = query.Where(p => p.JobId == jobId);

            var total = await query.CountAsync();
            var candidates = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new AdminCandidateResponse
                {
                    Id = p.Id,
                    UserId = p.UserId,
                    Email = p.User.Email,
                    JobId = p.JobId,
                    JobTitle = p.Job != null ? p.Job.Title : null,
                    Seniority = p.Seniority,
                    MatchingScore = p.MatchingScore,
                    Status = p.Status,
                    FileUrl = p.FileUrl,
                    Strengths = p.Strengths,
                    Gaps = p.Gaps,
                    ParsedSkillsJson = p.ParsedSkillsJson,
                    CreatedAt = p.CreatedAt ?? DateTime.UtcNow,
                    UpdatedAt = p.UpdatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                total,
                page,
                pageSize,
                data = candidates
            });
        }

        // GET /api/admin/candidates/{profileId}
        // Chi tiết 1 candidate profile
        [HttpGet("candidates/{profileId:guid}")]
        public async Task<ActionResult<AdminCandidateResponse>> GetCandidate(Guid profileId)
        {
            var profile = await _context.CandidateProfiles
                .Include(p => p.User)
                .Include(p => p.Job)
                .FirstOrDefaultAsync(p => p.Id == profileId);

            if (profile == null)
                return NotFound(new { message = "Profile không tồn tại." });

            return Ok(new AdminCandidateResponse
            {
                Id = profile.Id,
                UserId = profile.UserId,
                Email = profile.User?.Email ?? "",
                JobId = profile.JobId,
                JobTitle = profile.Job?.Title,
                Seniority = profile.Seniority,
                MatchingScore = profile.MatchingScore,
                Status = profile.Status,
                FileUrl = profile.FileUrl,
                Strengths = profile.Strengths,
                Gaps = profile.Gaps,
                ParsedSkillsJson = profile.ParsedSkillsJson,
                CreatedAt = profile.CreatedAt ?? DateTime.UtcNow,
                UpdatedAt = profile.UpdatedAt
            });
        }

        // DELETE /api/admin/users/{userId}
        [HttpDelete("users/{userId:guid}")]
        public async Task<IActionResult> DeleteUser(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound(new { message = "User không tồn tại." });

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Admin deleted user {UserId}", userId);
            return Ok(new { message = "Xóa user thành công." });
        }

        // PATCH /api/admin/users/{userId}/role
        [HttpPatch("users/{userId:guid}/role")]
        public async Task<IActionResult> UpdateUserRole(Guid userId, [FromBody] UpdateUserRoleRequest request)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound(new { message = "User không tồn tại." });

            if (Enum.TryParse<UserRole>(request.Role, true, out var roleEnum))
                user.Role = roleEnum;
            else
                return BadRequest(new { message = "Role không hợp lệ. Dùng: Candidate, Recruiter, Admin" });
            await _context.SaveChangesAsync();

            _logger.LogInformation("Admin updated role for user {UserId} to {Role}", userId, request.Role);
            return Ok(new { message = $"Đã cập nhật role thành {request.Role}." });
        }
    }

    public class UpdateUserRoleRequest
    {
        public string Role { get; set; } = null!;
    }
}