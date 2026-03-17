using Microsoft.AspNetCore.Mvc;
using The_Hirelo.DTOs.Requests;
using The_Hirelo.Services.Interfaces;

namespace The_Hirelo.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PasswordController : ControllerBase
{
    private readonly IPasswordService _passwordService;

    public PasswordController(IPasswordService passwordService)
    {
        _passwordService = passwordService;
    }

    /// <summary>
    /// Initiate forgot password flow
    /// </summary>
    /// <param name="request">Email address</param>
    /// <returns>Result message</returns>
    [HttpPost("forgot")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        try
        {
            var (success, message) = await _passwordService.ForgotPasswordAsync(request);
            return Ok(new { message });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PasswordController.ForgotPassword] Error: {ex.Message}");
            return StatusCode(500, new { message = $"Failed to initiate password reset: {ex.Message}" });
        }
    }

    /// <summary>
    /// Reset password with confirmation code
    /// </summary>
    /// <param name="request">Email, confirmation code, and new password</param>
    /// <returns>Reset result</returns>
    [HttpPost("reset")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        try
        {
            var (success, message) = await _passwordService.ResetPasswordAsync(request);

            if (!success)
                return BadRequest(new { message });

            return Ok(new { message });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PasswordController.ResetPassword] Error: {ex.Message}");
            return StatusCode(500, new { message = $"Password reset failed: {ex.Message}" });
        }
    }

    /// <summary>
    /// Change password for authenticated user
    /// </summary>
    /// <param name="request">Current password and new password</param>
    /// <returns>Change result</returns>
    [HttpPost("change")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        try
        {
            var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authHeader))
            {
                return Unauthorized(new { message = "Authorization header missing." });
            }

            var accessToken = authHeader.Replace("Bearer ", "").Trim();
            if (string.IsNullOrEmpty(accessToken))
            {
                return Unauthorized(new { message = "Invalid authorization token." });
            }

            var (success, message) = await _passwordService.ChangePasswordAsync(request, accessToken);

            if (!success)
                return BadRequest(new { message });

            return Ok(new { message });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PasswordController.ChangePassword] Error: {ex.Message}");
            return StatusCode(500, new { message = $"Password change failed: {ex.Message}" });
        }
    }
}
