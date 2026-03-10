using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using The_Hirelo.Data;
using The_Hirelo.Models;
using Amazon.SQS;
using Amazon.SQS.Model;
using System.Text.Json;
using The_Hirelo.DTOs.Requests;

namespace The_Hirelo.Controllers
{
    [ApiController]
    [Route("api/cv")]
    public class ParsedResultController : ControllerBase
    {
        private readonly HireloDbContext _context;
        private readonly IAmazonSQS _sqs;
        private readonly IConfiguration _config;
        private readonly ILogger<ParsedResultController> _logger;

        public ParsedResultController(
            HireloDbContext context,
            IAmazonSQS sqs,
            IConfiguration config,
            ILogger<ParsedResultController> logger)
        {
            _context = context;
            _sqs = sqs;
            _config = config;
            _logger = logger;
        }

        // POST /api/cv/parsed-result
        // Lambda gọi endpoint này sau khi parse xong
        [HttpPost("parsed-result")]
        public async Task<IActionResult> ReceiveParsedResult([FromBody] ParsedResultRequest request)
        {
            try
            {
                var profile = await _context.CandidateProfiles
                    .Include(p => p.User)
                    .Include(p => p.Job)
                        .ThenInclude(j => j!.Recruiter)
                    .FirstOrDefaultAsync(p => p.Id == request.ProfileId);

                if (profile == null)
                    return NotFound(new { message = "Profile not found." });

                // UPDATE profile với kết quả từ Lambda
                profile.Seniority = request.SeniorityEstimate;
                profile.ParsedSkillsJson = JsonSerializer.Serialize(new
                {
                    frontend = request.FrontendSkills,
                    backend = request.BackendSkills,
                    devops = request.DevopsSkills,
                    soft = request.SoftSkills
                });
                profile.Strengths = request.Strengths;
                profile.Gaps = request.Gaps;
                profile.MatchingScore = request.MatchingScore;
                profile.Status = request.Success ? CandidateProfileStatus.Done : CandidateProfileStatus.Failed;
                profile.UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

                await _context.SaveChangesAsync();

                _logger.LogInformation("Profile {Id} updated from Lambda | Score={Score}",
                    request.ProfileId, request.MatchingScore);

                // PUBLISH cv_parsed_done → NotificationWorker
                if (request.Success)
                {
                    var doneQueueUrl = _config["AWS:SQS:CVParsedDoneQueueUrl"]!;
                    await _sqs.SendMessageAsync(new SendMessageRequest
                    {
                        QueueUrl = doneQueueUrl,
                        MessageBody = JsonSerializer.Serialize(new
                        {
                            profileId = profile.Id.ToString(),
                            candidateId = profile.UserId.ToString(),
                            recruiterId = profile.Job?.Recruiter?.UserId.ToString() ?? "",
                            candidateEmail = profile.User?.Email ?? "",
                            candidateName = profile.User?.Email?.Split('@')[0] ?? "Candidate",
                            matchingScore = request.MatchingScore
                        })
                    });
                }

                return Ok(new { message = "Profile updated successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update profile from Lambda");
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }

}