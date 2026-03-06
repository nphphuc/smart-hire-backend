using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using The_Hirelo.Services.Interfaces;
using The_Hirelo.Extensions;
using The_Hirelo.Repositories.Interfaces;
using The_Hirelo.DTOs.Requests;
using The_Hirelo.Enums;
using The_Hirelo.Models;

namespace The_Hirelo.Controllers
{
    [ApiController]
    [Route("api/recruiter")]
    [Authorize]
    public class RecruiterController : ControllerBase
    {
        private readonly IJobService _jobService;
        private readonly ICandidateService _candidateService;
        private readonly IComparisonService _comparisonService;
        private readonly IUserRepository _userRepository;
        private readonly ICompanyService _companyService;

        public RecruiterController(IJobService jobService, ICandidateService candidateService, IComparisonService comparisonService, IUserRepository userRepository, ICompanyService companyService)
        {
            _jobService = jobService;
            _candidateService = candidateService;
            _comparisonService = comparisonService;
            _userRepository = userRepository;
            _companyService = companyService;
        }

        // GET /api/recruiter/jobs
        [HttpGet("jobs")]
        public async Task<IActionResult> GetJobs()
        {
            var cognitoSub = User.GetCognitoSub();
            Guid? recruiterProfileId = null;
            if (!string.IsNullOrEmpty(cognitoSub))
            {
                var user = await _userRepository.GetByCognitoSubAsync(cognitoSub);
                if (user?.RecruiterProfile != null)
                    recruiterProfileId = user.RecruiterProfile.Id;
            }

            var jobs = await _jobService.GetAllJobsAsync(recruiterProfileId);
            return Ok(jobs);
        }

        // POST /api/recruiter/jobs
        [HttpPost("jobs")]
        public async Task<IActionResult> CreateJob([FromForm] CreateJobRequest dto)
        {
            var cognitoSub = User.GetCognitoSub();
            if (string.IsNullOrEmpty(cognitoSub)) return Forbid();
            var user = await _userRepository.GetByCognitoSubAsync(cognitoSub);
            if (user == null || user.RecruiterProfile == null) return Forbid();

            var jd = Request.Form.Files.FirstOrDefault();
            var job = await _jobService.CreateJobAsync(user.RecruiterProfile.Id, dto, jd);
            return Ok(job);
        }

        // GET /api/recruiter/jobs/{jobId}
        [HttpGet("jobs/{jobId}")]
        public async Task<IActionResult> GetJob(Guid jobId)
        {
            var job = await _jobService.GetJobByIdAsync(jobId);
            if (job == null) return NotFound();
            return Ok(job);
        }

        // PUT /api/recruiter/jobs/{jobId}
        [HttpPut("jobs/{jobId}")]
        public async Task<IActionResult> UpdateJob(Guid jobId, [FromForm] UpdateJobRequest dto)
        {
            var cognitoSub = User.GetCognitoSub();
            if (string.IsNullOrEmpty(cognitoSub)) return Forbid();
            var user = await _userRepository.GetByCognitoSubAsync(cognitoSub);
            if (user == null || user.RecruiterProfile == null) return Forbid();

            var existing = await _jobService.GetJobByIdAsync(jobId);
            if (existing == null) return NotFound();
            if (existing.RecruiterProfileId != user.RecruiterProfile.Id) return Forbid();

            var jd = Request.Form.Files.FirstOrDefault();
            var updated = await _jobService.UpdateJobAsync(jobId, dto, jd);
            return Ok(updated);
        }

        // DELETE /api/recruiter/jobs/{jobId}
        [HttpDelete("jobs/{jobId}")]
        public async Task<IActionResult> DeleteJob(Guid jobId)
        {
            var cognitoSub = User.GetCognitoSub();
            if (string.IsNullOrEmpty(cognitoSub)) return Forbid();
            var user = await _userRepository.GetByCognitoSubAsync(cognitoSub);
            if (user == null || user.RecruiterProfile == null) return Forbid();

            var existing = await _jobService.GetJobByIdAsync(jobId);
            if (existing == null) return NotFound();
            if (existing.RecruiterProfileId != user.RecruiterProfile.Id) return Forbid();

            await _jobService.DeleteJobAsync(jobId);
            return Ok(new { message = "Deleted" });
        }

        // POST /api/recruiter/jobs/{jobId}/jd
        [HttpPost("jobs/{jobId}/jd")]
        public async Task<IActionResult> UploadJobDescription(Guid jobId)
        {
            var file = Request.Form.Files.FirstOrDefault();
            if (file == null) return BadRequest(new { message = "No file uploaded" });

            await _jobService.UploadJobDescriptionAsync(jobId, file);
            return Ok(new { message = "Uploaded" });
        }

        // GET /api/recruiter/jobs/{jobId}/jd
        [HttpGet("jobs/{jobId}/jd")]
        public async Task<IActionResult> GetJobDescription(Guid jobId)
        {
            var job = await _jobService.GetJobByIdAsync(jobId);
            if (job == null) return NotFound();
            if (string.IsNullOrEmpty(job.JdFileUrl)) return NotFound(new { message = "JD not found" });
            return Ok(new { jdUrl = job.JdFileUrl });
        }

        // DELETE /api/recruiter/jobs/{jobId}/jd
        [HttpDelete("jobs/{jobId}/jd")]
        public async Task<IActionResult> DeleteJobDescription(Guid jobId)
        {
            await _jobService.DeleteJobDescriptionAsync(jobId);
            return Ok(new { message = "Deleted" });
        }

        // GET /api/recruiter/jobs/{jobId}/candidates
        [HttpGet("jobs/{jobId}/candidates")]
        public async Task<IActionResult> GetCandidatesByJob(Guid jobId)
        {
            var list = await _candidateService.GetCandidatesByJobAsync(jobId);
            return Ok(list);
        }

        // GET /api/recruiter/candidates/{candidateId}
        [HttpGet("candidates/{candidateId}")]
        public async Task<IActionResult> GetCandidate(Guid candidateId)
        {
            var detail = await _candidateService.GetCandidateDetailsAsync(candidateId);
            if (detail == null) return NotFound();
            return Ok(detail);
        }

        // PUT /api/recruiter/candidates/{candidateId}/status
        [HttpPut("candidates/{candidateId}/status")]
        public async Task<IActionResult> UpdateCandidateStatus(Guid candidateId, [FromBody] UpdateCandidateStatusRequest dto)
        {
            await _candidateService.UpdateCandidateStatusAsync(candidateId, dto.Status);
            return Ok(new { message = "Status updated" });
        }

        // POST /api/recruiter/jobs/{jobId}/compare
        [HttpPost("jobs/{jobId}/compare")]
        public async Task<IActionResult> CompareCandidates(Guid jobId, [FromBody] The_Hirelo.DTOs.Requests.CompareCandidatesRequest dto)
        {
            var result = await _comparisonService.CompareCandidatesAsync(dto.CandidateIds);
            return Ok(result);
        }

        // GET /api/recruiter/dashboard/overview
        [HttpGet("dashboard/overview")]
        public IActionResult DashboardOverview()
        {
            // Implementation detail depends on services. Return placeholder.
            return Ok(new { message = "Overview not implemented" });
        }

        // GET /api/recruiter/dashboard/jobs/{jobId}
        [HttpGet("dashboard/jobs/{jobId}")]
        public IActionResult DashboardJob(Guid jobId)
        {
            return Ok(new { message = "Job dashboard not implemented" });
        }

        // GET /api/recruiter/companies/me
        [HttpGet("companies/me")]
        public async Task<IActionResult> GetMyCompany()
        {
            var cognitoSub = User.GetCognitoSub();
            if (string.IsNullOrEmpty(cognitoSub)) return Forbid();
            var user = await _userRepository.GetByCognitoSubAsync(cognitoSub);
            if (user == null || user.RecruiterProfile == null) return NotFound();

            var company = await _companyService.GetByRecruiterProfileIdAsync(user.RecruiterProfile.Id);
            if (company == null) return NotFound();
            return Ok(company);
        }

        // PUT /api/recruiter/companies/me
        [HttpPut("companies/me")]
        public async Task<IActionResult> UpdateMyCompany([FromBody] Company dto)
        {
            var cognitoSub = User.GetCognitoSub();
            if (string.IsNullOrEmpty(cognitoSub)) return Forbid();
            var user = await _userRepository.GetByCognitoSubAsync(cognitoSub);
            if (user == null || user.RecruiterProfile == null) return NotFound();

            var company = await _companyService.GetByRecruiterProfileIdAsync(user.RecruiterProfile.Id);
            if (company == null) return NotFound();

            company.Name = dto.Name ?? company.Name;
            var updated = await _companyService.UpdateAsync(company);
            return Ok(updated);
        }

        // GET /api/recruiter/companies/{companyId}
        [HttpGet("companies/{companyId}")]
        public async Task<IActionResult> GetCompany(Guid companyId)
        {
            var company = await _companyService.GetByIdAsync(companyId);
            if (company == null) return NotFound();
            return Ok(company);
        }

        // PUT /api/recruiter/companies/{companyId}
        [HttpPut("companies/{companyId}")]
        public async Task<IActionResult> UpdateCompany(Guid companyId, [FromBody] Company dto)
        {
            var company = await _companyService.GetByIdAsync(companyId);
            if (company == null) return NotFound();
            company.Name = dto.Name ?? company.Name;
            var updated = await _companyService.UpdateAsync(company);
            return Ok(updated);
        }
    }
}
