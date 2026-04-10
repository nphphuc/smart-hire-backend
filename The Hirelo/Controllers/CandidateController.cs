using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using The_Hirelo.Extensions;
using The_Hirelo.Repositories.Interfaces;
using The_Hirelo.Services.Interfaces;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using System.Text.Json;

namespace The_Hirelo.Controllers
{
    [ApiController]
    [Authorize]
    public class CandidateController : ControllerBase
    {
        private readonly ICandidateService _candidateService;
        private readonly IUserRepository _userRepository;
        private readonly IJdRepository _jdRepository;
        private readonly IAmazonLambda _lambda;
        private readonly ILogger<CandidateController> _logger;

        public CandidateController(
            ICandidateService candidateService,
            IUserRepository userRepository,
            IJdRepository jdRepository,
            IAmazonLambda lambda,
            ILogger<CandidateController> logger)
        {
            _candidateService = candidateService;
            _userRepository = userRepository;
            _jdRepository = jdRepository;
            _lambda = lambda;
            _logger = logger;
        }

        // -------------------------------------------------------
        // CANDIDATE role: Tự xem CV của mình
        // GET /api/candidate/me/cv
        // -------------------------------------------------------
        [HttpGet("api/candidate/me/cv")]
        public async Task<IActionResult> GetMyCv()
        {
            var cognitoSub = User.GetCognitoSub();
            if (string.IsNullOrEmpty(cognitoSub)) return Forbid();

            var user = await _userRepository.GetByCognitoSubAsync(cognitoSub);
            if (user == null || user.CandidateProfile == null)
                return NotFound(new { message = "Candidate profile not found." });

            // candidateId trong DynamoDB là ProfileId của candidate
            var candidateId = user.CandidateProfile.Id.ToString();

            var cv = await _candidateService.GetLatestCvByCandidateIdAsync(candidateId);
            if (cv == null)
                return NotFound(new { message = "No CV found for this candidate." });

            return Ok(cv);
        }

        // GET /api/candidate/me/cvs  (tất cả các lần upload)
        [HttpGet("api/candidate/me/cvs")]
        public async Task<IActionResult> GetAllMyCvs()
        {
            var cognitoSub = User.GetCognitoSub();
            if (string.IsNullOrEmpty(cognitoSub)) return Forbid();

            var user = await _userRepository.GetByCognitoSubAsync(cognitoSub);
            if (user == null || user.CandidateProfile == null)
                return NotFound(new { message = "Candidate profile not found." });

            var candidateId = user.CandidateProfile.Id.ToString();
            var cvs = await _candidateService.GetAllCvsByCandidateIdAsync(candidateId);
            return Ok(cvs);
        }

        // -------------------------------------------------------
        // ADMIN role: Xem CV của bất kỳ candidate nào
        // GET /api/admin/candidates/{candidateId}/cv
        // -------------------------------------------------------
        [HttpGet("api/admin/candidates/{candidateId}/cv")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminGetCandidateCv(string candidateId)
        {
            var cv = await _candidateService.GetLatestCvByCandidateIdAsync(candidateId);
            if (cv == null)
                return NotFound(new { message = $"No CV found for candidateId: {candidateId}" });

            return Ok(cv);
        }

        // GET /api/admin/candidates/{candidateId}/cvs  (tất cả)
        [HttpGet("api/admin/candidates/{candidateId}/cvs")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminGetAllCandidateCvs(string candidateId)
        {
            var cvs = await _candidateService.GetAllCvsByCandidateIdAsync(candidateId);
            return Ok(cvs);
        }

        // -------------------------------------------------------
        // JD Parse Result từ DynamoDB
        // GET /api/jobs/{jobId}/jd-parse  — Candidate hoặc Recruiter đều có thể xem
        // -------------------------------------------------------
        [HttpGet("api/jobs/{jobId}/jd-parse")]
        public async Task<IActionResult> GetJdParseResult(Guid jobId)
        {
            var result = await _jdRepository.GetJdByJobIdAsync(jobId.ToString());
            if (result == null)
                return NotFound(new { message = "JD parse result not found for this job." });
            return Ok(result);
        }

        // GET /api/jobs/jd-parse  — Lấy tất cả JD parse results
        [HttpGet("api/jobs/jd-parse")]
        public async Task<IActionResult> GetAllJdParseResults()
        {
            var results = await _jdRepository.GetAllJdsAsync();
            return Ok(results);
        }

        // POST /api/candidate/me/refresh-suggestions
        // Re-invokes the job_suggestion_engine Lambda for the authenticated candidate,
        // causing it to re-run ANN search across all job_embeddings and push fresh
        // suggestions to the candidate dashboard via AppSync.
        [HttpPost("api/candidate/me/refresh-suggestions")]
        public async Task<IActionResult> RefreshJobSuggestions()
        {
            var cognitoSub = User.GetCognitoSub();
            if (string.IsNullOrEmpty(cognitoSub)) return Forbid();

            var user = await _userRepository.GetByCognitoSubAsync(cognitoSub);
            if (user == null || user.CandidateProfile == null)
                return NotFound(new { message = "Candidate profile not found." });

            var lambdaFunctionName = System.Environment.GetEnvironmentVariable("JOB_SUGGESTION_ENGINE_FUNCTION_NAME")
                ?? "job_suggestion_engine";

            // Minimal payload: profile_id is the Cognito sub. The Lambda reads the
            // stored CV vector + masked_cv_text from pgvector/DynamoDB automatically.
            var payload = JsonSerializer.Serialize(new
            {
                profile_id = cognitoSub,
                trigger_source = "manual_refresh"
            });

            try
            {
                await _lambda.InvokeAsync(new InvokeRequest
                {
                    FunctionName = lambdaFunctionName,
                    InvocationType = "Event",   // fire-and-forget (async)
                    Payload = payload
                });

                return Ok(new { message = "Suggestion refresh triggered. New matches will appear shortly via AppSync." });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Suggestion refresh Lambda invoke failed for candidate {CognitoSub}", cognitoSub);
                return StatusCode(500, new { message = "Failed to trigger suggestion refresh." });
            }
        }
    }
}
