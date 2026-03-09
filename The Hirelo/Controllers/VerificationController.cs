using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using The_Hirelo.Services.Interfaces;
using The_Hirelo.Extensions;
using The_Hirelo.Repositories.Interfaces;
using The_Hirelo.DTOs.Requests;
using The_Hirelo.DTOs.Responses;
using System.Text.Json;

namespace The_Hirelo.Controllers
{
    [ApiController]
    [Route("api/verification")]
    [Authorize]
    public class VerificationController : ControllerBase
    {
        private readonly IRecruiterVerificationService _verificationService;
        private readonly IUserRepository _userRepository;

        public VerificationController(
            IRecruiterVerificationService verificationService,
            IUserRepository userRepository)
        {
            _verificationService = verificationService;
            _userRepository = userRepository;
        }

        // POST /api/verification/submit
        [HttpPost("submit")]
        public async Task<IActionResult> SubmitVerification([FromForm] RecruiterVerificationRequest request)
        {
            var cognitoSub = User.GetCognitoSub();
            if (string.IsNullOrEmpty(cognitoSub)) return Forbid();

            var user = await _userRepository.GetByCognitoSubAsync(cognitoSub);
            if (user == null) return Forbid();

            var verificationId = await _verificationService.SubmitAsync(user.Id, request);
            return Ok(new { id = verificationId, message = "Verification submitted successfully" });
        }

        // GET /api/verification/all
        [HttpGet("all")]
        //[Authorize(Roles = "Admin")]
        [Authorize]
        public async Task<IActionResult> GetAllVerifications()
        {
            var verifications = await _verificationService.GetAllAsync();
            var responses = verifications.Select(v => MapToResponse(v)).ToList();
            return Ok(responses);
        }

        // GET /api/verification/{userId}
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetVerificationByUserId(Guid userId)
        {
            var verification = await _verificationService.GetByUserIdAsync(userId);
            if (verification == null) return NotFound();
            
            var response = MapToResponse(verification);
            return Ok(response);
        }

        // POST /api/verification/{verificationId}/approve
        [HttpPost("{verificationId}/approve")]
        //[Authorize(Roles = "Admin")]
        [Authorize]
        public async Task<IActionResult> ApproveVerification(Guid verificationId)
        {
            var result = await _verificationService.ApproveVerificationAsync(verificationId);
            if (!result) return NotFound();
            return Ok(new { message = "Verification approved and recruiter role assigned" });
        }

        // POST /api/verification/{verificationId}/reject
        [HttpPost("{verificationId}/reject")]
        //[Authorize(Roles = "Admin")]
        [Authorize]
        public async Task<IActionResult> RejectVerification(Guid verificationId)
        {
            var result = await _verificationService.RejectVerificationAsync(verificationId);
            if (!result) return NotFound();
            return Ok(new { message = "Verification rejected" });
        }

        // POST /api/verification/{userId}/remove-recruiter-role
        [HttpPost("{userId}/remove-recruiter-role")]
        //[Authorize(Roles = "Admin")]
        [Authorize]
        public async Task<IActionResult> RemoveRecruiterRole(Guid userId)
        {
            var result = await _verificationService.RemoveRecruiterRoleAsync(userId);
            if (!result) return NotFound();
            return Ok(new { message = "Recruiter role removed" });
        }

        private RecruiterVerificationResponse MapToResponse(The_Hirelo.Models.RecruiterVerification verification)
        {
            object? images = null;
            try
            {
                images = JsonSerializer.Deserialize<object>(verification.ImagesJson);
            }
            catch
            {
                // If JSON parsing fails, keep images as null
            }

            return new RecruiterVerificationResponse
            {
                Id = verification.Id,
                UserId = verification.UserId,
                CompanyName = verification.CompanyName,
                CompanyTaxCode = verification.CompanyTaxCode,
                RecruiterEmail = verification.RecruiterEmail,
                Images = images,
                Status = verification.Status,
                CreatedAt = verification.CreatedAt,
                UpdatedAt = verification.UpdatedAt
            };
        }
    }
}
