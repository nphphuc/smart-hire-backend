using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using The_Hirelo.Extensions;
using The_Hirelo.Repositories.Interfaces;
using The_Hirelo.Services.Interfaces;

namespace The_Hirelo.Controllers
{
    [ApiController]
    [Authorize]
    public class CandidateController : ControllerBase
    {
        private readonly ICandidateService _candidateService;
        private readonly IUserRepository _userRepository;
        private readonly IJdRepository _jdRepository;

        public CandidateController(ICandidateService candidateService, IUserRepository userRepository, IJdRepository jdRepository)
        {
            _candidateService = candidateService;
            _userRepository = userRepository;
            _jdRepository = jdRepository;
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
    }
}
