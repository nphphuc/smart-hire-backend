using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using The_Hirelo.DTOs.Requests;
using The_Hirelo.Extensions;
using The_Hirelo.Repositories.Interfaces;
using The_Hirelo.Services.Interfaces;

namespace The_Hirelo.Controllers
{
    [ApiController]
    [Authorize]
    public class ApplicationController : ControllerBase
    {
        private readonly IApplicationService _applicationService;
        private readonly IUserRepository _userRepository;
        private readonly IJobRepository _jobRepository;

        public ApplicationController(
            IApplicationService applicationService,
            IUserRepository userRepository,
            IJobRepository jobRepository)
        {
            _applicationService = applicationService;
            _userRepository = userRepository;
            _jobRepository = jobRepository;
        }

        /// <summary>
        /// Candidate Apply for a Job
        /// POST /api/applications
        /// </summary>
        [HttpPost("api/applications")]
        public async Task<IActionResult> CreateApplication([FromBody] CreateApplicationRequest request)
        {
            var cognitoSub = User.GetCognitoSub();
            if (string.IsNullOrEmpty(cognitoSub))
                return Forbid();

            var user = await _userRepository.GetByCognitoSubAsync(cognitoSub);
            if (user?.CandidateProfile == null)
                return BadRequest(new { message = "Only candidates can apply for jobs." });

            try
            {
                var application = await _applicationService.CreateApplicationAsync(
                    user.CandidateProfile.Id,
                    request);

                if (application == null)
                    return BadRequest(new { message = "Failed to create application." });

                return CreatedAtAction(
                    nameof(GetApplicationById),
                    new { applicationId = application.ApplicationId },
                    new
                    {
                        applicationId = application.ApplicationId,
                        status = application.Status,
                        appliedAt = application.AppliedAt
                    });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Get Application by ID
        /// GET /api/applications/{applicationId}
        /// </summary>
        [HttpGet("api/applications/{applicationId}")]
        public async Task<IActionResult> GetApplicationById(Guid applicationId)
        {
            var application = await _applicationService.GetApplicationByIdAsync(applicationId);
            if (application == null)
                return NotFound(new { message = "Application not found." });

            return Ok(application);
        }

        /// <summary>
        /// Recruiter: Get All Applications for a Job
        /// GET /api/jobs/{jobId}/applications
        /// </summary>
        [HttpGet("api/jobs/{jobId}/applications")]
        [Authorize(Roles = "Recruiter")]
        public async Task<IActionResult> GetApplicationsByJob(Guid jobId)
        {
            var cognitoSub = User.GetCognitoSub();
            if (string.IsNullOrEmpty(cognitoSub))
                return Forbid();

            var user = await _userRepository.GetByCognitoSubAsync(cognitoSub);
            if (user?.RecruiterProfile == null)
                return Forbid();

            // Verify recruiter owns this job
            var job = await _jobRepository.GetByIdAsync(jobId);
            if (job == null)
                return NotFound(new { message = "Job not found." });

            if (job.RecruiterId != user.RecruiterProfile.Id)
                return BadRequest(new { message = "You do not have permission to view applications for this job." });

            var applications = await _applicationService.GetApplicationsByJobIdAsync(jobId);
            return Ok(new
            {
                jobId = jobId,
                totalApplications = applications.Count(),
                applications = applications
            });
        }

        /// <summary>
        /// Candidate: Get My Applications
        /// GET /api/candidate/applications
        /// </summary>
        [HttpGet("api/candidate/applications")]
        public async Task<IActionResult> GetMyApplications()
        {
            var cognitoSub = User.GetCognitoSub();
            if (string.IsNullOrEmpty(cognitoSub))
                return Forbid();

            var user = await _userRepository.GetByCognitoSubAsync(cognitoSub);
            if (user?.CandidateProfile == null)
                return Forbid();

            var applications = await _applicationService.GetApplicationsByCandidateIdAsync(user.CandidateProfile.Id);
            return Ok(applications);
        }

        /// <summary>
        /// Update Application Status
        /// PATCH /api/applications/{applicationId}
        /// </summary>
        [HttpPatch("api/applications/{applicationId}")]
        [Authorize(Roles = "Recruiter")]
        public async Task<IActionResult> UpdateApplicationStatus(
            Guid applicationId,
            [FromBody] UpdateApplicationStatusRequest request)
        {
            var cognitoSub = User.GetCognitoSub();
            if (string.IsNullOrEmpty(cognitoSub))
                return Forbid();

            var user = await _userRepository.GetByCognitoSubAsync(cognitoSub);
            if (user?.RecruiterProfile == null)
                return Forbid();

            var application = await _applicationService.GetApplicationByIdAsync(applicationId);
            if (application == null)
                return NotFound(new { message = "Application not found." });

            // Verify recruiter owns the job
            var job = await _jobRepository.GetByIdAsync(application.JobId);
            if (job?.RecruiterId != user.RecruiterProfile.Id)
                return BadRequest(new { message = "You do not have permission to update this application." });

            var success = await _applicationService.UpdateApplicationStatusAsync(applicationId, request.Status);
            if (!success)
                return BadRequest(new { message = "Failed to update application status." });

            var updatedApplication = await _applicationService.GetApplicationByIdAsync(applicationId);
            return Ok(new
            {
                message = "Application status updated successfully.",
                application = updatedApplication
            });
        }

        /// <summary>
        /// Delete Application
        /// DELETE /api/applications/{applicationId}
        /// </summary>
        [HttpDelete("api/applications/{applicationId}")]
        public async Task<IActionResult> DeleteApplication(Guid applicationId)
        {
            var cognitoSub = User.GetCognitoSub();
            if (string.IsNullOrEmpty(cognitoSub))
                return Forbid();

            var user = await _userRepository.GetByCognitoSubAsync(cognitoSub);
            if (user?.CandidateProfile == null && user?.RecruiterProfile == null)
                return Forbid();

            var application = await _applicationService.GetApplicationByIdAsync(applicationId);
            if (application == null)
                return NotFound(new { message = "Application not found." });

            // Allow candidate to delete their own application or recruiter to delete from their job
            if (user.CandidateProfile?.Id == application.CandidateId)
            {
                // Candidate deleting their own application
            }
            else if (user.RecruiterProfile != null)
            {
                var job = await _jobRepository.GetByIdAsync(application.JobId);
                if (job?.RecruiterId != user.RecruiterProfile.Id)
                    return Forbid();
            }
            else
            {
                return Forbid();
            }

            var success = await _applicationService.DeleteApplicationAsync(applicationId);
            if (!success)
                return BadRequest(new { message = "Failed to delete application." });

            return Ok(new { message = "Application deleted successfully." });
        }
    }
}
