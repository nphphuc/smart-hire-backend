using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using The_Hirelo.DTOs.Requests;
using The_Hirelo.DTOs.Responses;
using The_Hirelo.Enums;
using The_Hirelo.Services.Interfaces;

namespace The_Hirelo.Controllers;

[ApiController]
[Route("api/[controller]")]
[Microsoft.AspNetCore.Authorization.Authorize]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IRecruiterVerificationService _recruiterVerificationService;

    public UserController(IUserService userService, IRecruiterVerificationService recruiterVerificationService)
    {
        _userService = userService;
        _recruiterVerificationService = recruiterVerificationService;
    }

    /// <summary>
    /// Get current user information
    /// </summary>
    /// <returns>User info with ID, email, role, and creation date</returns>
    [HttpGet("me")]
    public async Task<ActionResult<UserInfoResponse>> GetCurrentUser()
    {
        try
        {
            var cognitoSub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(cognitoSub))
            {
                return Unauthorized(new { message = "Invalid token" });
            }

            var user = await _userService.GetCurrentUserAsync(cognitoSub);

            if (user == null)
            {
                return NotFound(new { message = "User not found. Please login again." });
            }

            return Ok(user);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[UserController.GetCurrentUser] Error: {ex.Message}");
            return StatusCode(500, new { message = $"Failed to get user info: {ex.Message}" });
        }
    }

    /// <summary>
    /// Update user role (admin only)
    /// </summary>
    /// <param name="userId">User ID to update</param>
    /// <param name="request">New role</param>
    /// <returns>Update result</returns>
    [HttpPut("{userId}/role")]
    public async Task<IActionResult> UpdateUserRole(Guid userId, [FromBody] UpdateRoleRequest request)
    {
        try
        {
            var currentCognitoSub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value;

            var (success, message) = await _userService.UpdateUserRoleAsync(userId, request.Role, currentCognitoSub);

            if (!success)
            {
                return BadRequest(new { message });
            }

            return Ok(new { message });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[UserController.UpdateUserRole] Error: {ex.Message}");
            return StatusCode(500, new { message = $"Failed to update user role: {ex.Message}" });
        }
    }

    /// <summary>
    /// Submit recruiter verification
    /// </summary>
    /// <param name="request">Verification documents and details</param>
    /// <returns>Verification ID</returns>
    [HttpPost("verification/submit")]
    public async Task<IActionResult> SubmitRecruiterVerification([FromForm] RecruiterVerificationRequest request)
    {
        try
        {
            var cognitoSub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(cognitoSub))
            {
                return Unauthorized(new { message = "Invalid token." });
            }

            var verificationId = await _userService.SubmitRecruiterVerificationAsync(
                cognitoSub, 
                request, 
                _recruiterVerificationService);

            return Ok(new { verificationId });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { message = "Invalid token." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[UserController.SubmitRecruiterVerification] Error: {ex.Message}");
            return StatusCode(500, new { message = $"Submission failed: {ex.Message}" });
        }
    }
}
