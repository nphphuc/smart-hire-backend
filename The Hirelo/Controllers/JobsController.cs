using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using The_Hirelo.Services.Interfaces;

namespace The_Hirelo.Controllers
{
    /// <summary>Authenticated candidates: browse jobs from RDS catalog.</summary>
    [ApiController]
    [Route("api/jobs")]
    [Authorize]
    public class JobsController : ControllerBase
    {
        private readonly IJobService _jobService;

        public JobsController(IJobService jobService)
        {
            _jobService = jobService;
        }

        [HttpGet("catalog")]
        public async Task<IActionResult> GetCatalog()
        {
            var jobs = await _jobService.GetCandidateJobCatalogAsync();
            return Ok(jobs);
        }

        [HttpGet("catalog/{jobId:guid}")]
        public async Task<IActionResult> GetCatalogJob(Guid jobId)
        {
            var job = await _jobService.GetCandidateCatalogJobAsync(jobId);
            if (job == null) return NotFound(new { message = "Job not found." });
            return Ok(job);
        }
    }
}
