using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using The_Hirelo.Services.Interfaces;
using The_Hirelo.Extensions;
using The_Hirelo.Repositories.Interfaces;
using The_Hirelo.DTOs.Requests;
using The_Hirelo.Enums;
using The_Hirelo.Models;
using Microsoft.AspNetCore.Mvc.Filters;
using The_Hirelo.Data;

namespace The_Hirelo.Controllers
{
    [ApiController]
    [Route("api/recruiter")]
    [Authorize]
    [TypeFilter(typeof(RecruiterOnlyFilter))]
    public class RecruiterController : ControllerBase
    {
        private readonly IJobService _jobService;
        private readonly ICandidateService _candidateService;
        private readonly IComparisonService _comparisonService;
        private readonly IUserRepository _userRepository;
        private readonly ICompanyService _companyService;
        private readonly IJdRepository _jdRepository;
        private readonly HireloDbContext _db;

        public RecruiterController(IJobService jobService, ICandidateService candidateService, IComparisonService comparisonService, IUserRepository userRepository, ICompanyService companyService, IJdRepository jdRepository, HireloDbContext db)
        {
            _jobService = jobService;
            _candidateService = candidateService;
            _comparisonService = comparisonService;
            _userRepository = userRepository;
            _companyService = companyService;
            _jdRepository = jdRepository;
            _db = db;
        }

        // POST /api/recruiter/profile
        [HttpPost("profile")]
        public async Task<IActionResult> CreateRecruiterProfile([FromBody] CreateRecruiterProfileRequest dto)
        {
            var cognitoSub = User.GetCognitoSub();
            if (string.IsNullOrEmpty(cognitoSub)) return Forbid();
            
            var user = await _userRepository.GetByCognitoSubAsync(cognitoSub);
            if (user == null) return NotFound("User not found");
            
            // Check if user already has a recruiter profile
            if (user.RecruiterProfile != null)
                return BadRequest("User already has a recruiter profile");

            // Create company
            var company = new Company
            {
                Id = Guid.NewGuid(),
                Name = dto.CompanyName,
                TaxCode = dto.TaxCode,
                CreatedAt = DateTime.UtcNow
            };

            // Create recruiter profile
            var recruiterProfile = new RecruiterProfile
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Company = company,
                IsVerified = false
            };

            _db.Companies.Add(company);
            _db.RecruiterProfiles.Add(recruiterProfile);
            await _db.SaveChangesAsync();

            return Ok(new { 
                recruiterProfileId = recruiterProfile.Id, 
                companyId = company.Id,
                companyName = company.Name,
                message = "Recruiter profile created successfully" 
            });
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

        // GET /api/recruiter/jobs/{jobId}/jd-parse
        // Trả về kết quả parse JD từ DynamoDB (được Lambda ghi sau khi xử lý file JD)
        [HttpGet("jobs/{jobId}/jd-parse")]
        public async Task<IActionResult> GetJdParseResult(Guid jobId)
        {
            var result = await _jdRepository.GetJdByJobIdAsync(jobId.ToString());
            if (result == null)
                return NotFound(new { message = "JD parse result not found for this job." });
            return Ok(result);
        }

        // GET /api/recruiter/jobs/jd-parse
        // Lấy tất cả JD parse results (Recruiter only)
        [HttpGet("jobs/jd-parse")]
        public async Task<IActionResult> GetAllJdParseResults()
        {
            var results = await _jdRepository.GetAllJdsAsync();
            return Ok(results);
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

        // GET /api/recruiter/candidates/{candidateId}/cv
        // Recruiter chỉ được đọc CV nếu candidate đã apply vào ít nhất 1 job của recruiter đó
        [HttpGet("candidates/{candidateId}/cv")]
        public async Task<IActionResult> GetCandidateCv(Guid candidateId)
        {
            var cognitoSub = User.GetCognitoSub();
            if (string.IsNullOrEmpty(cognitoSub)) return Forbid();

            var user = await _userRepository.GetByCognitoSubAsync(cognitoSub);
            if (user == null || user.RecruiterProfile == null) return Forbid();

            // Kiểm tra: candidate đã có InterviewSession với job nào của recruiter này chưa?
            var recruiterProfileId = user.RecruiterProfile.Id;
            var hasApplied = await _candidateService.HasCandidateAppliedToRecruiterJobAsync(candidateId, recruiterProfileId);
            if (!hasApplied)
                return Forbid(); // Candidate chưa apply -> Recruiter không được xem CV

            var cv = await _candidateService.GetLatestCvByCandidateIdAsync(candidateId.ToString());
            if (cv == null)
                return NotFound(new { message = "No CV found for this candidate." });

            return Ok(cv);
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

        // POST /api/recruiter/
    }

    // Action filter that ensures the authenticated user has UserRole.Recruiter
    public class RecruiterOnlyFilter : IAsyncActionFilter
    {
        private readonly IUserRepository _userRepository;

        public RecruiterOnlyFilter(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var userPrincipal = context.HttpContext.User;
            var cognitoSub = userPrincipal.GetCognitoSub();
            if (string.IsNullOrEmpty(cognitoSub))
            {
                context.Result = new ForbidResult();
                return;
            }

            var user = await _userRepository.GetByCognitoSubAsync(cognitoSub);
            if (user == null || user.Role != UserRole.Recruiter)
            {
                context.Result = new ForbidResult();
                return;
            }

            await next();
        }
    }
}
