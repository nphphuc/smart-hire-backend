using Microsoft.AspNetCore.Mvc;
using The_Hirelo.DTOs.Requests;
using The_Hirelo.DTOs.Responses;
using The_Hirelo.Services.Interfaces;

namespace The_Hirelo.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    /// <param name="request">Registration request with email, password, and fullName</param>
    /// <returns>Registration result with userSub</returns>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var (success, message, userSub) = await _authService.RegisterAsync(request);

            if (!success)
            {
                return BadRequest(new { message });
            }

            return Ok(new
            {
                message,
                userSub,
                confirmed = false
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AuthController.Register] Error: {ex.Message}");
            return StatusCode(500, new { message = $"Registration failed: {ex.Message}" });
        }
    }

    /// <summary>
    /// Login with email and password
    /// </summary>
    /// <param name="request">Login request with email and password</param>
    /// <returns>Access token, ID token, and refresh token</returns>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var response = await _authService.LoginAsync(request);
            return Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { message = "Incorrect email or password." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AuthController.Login] Error: {ex.Message}");
            return StatusCode(500, new { message = $"Login failed: {ex.Message}" });
        }
    }

    ///// <summary>
    ///// Logout user
    ///// </summary>
    ///// <returns>Logout confirmation</returns>
    //[HttpPost("logout")]
    //[Microsoft.AspNetCore.Authorization.Authorize]
    //public async Task<IActionResult> Logout()
    //{
    //    try
    //    {
    //        var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
    //        if (string.IsNullOrEmpty(authHeader))
    //        {
    //            return Unauthorized(new { message = "Authorization header missing." });
    //        }

    //        var accessToken = authHeader.Replace("Bearer ", "").Trim();
    //        if (string.IsNullOrEmpty(accessToken))
    //        {
    //            return Unauthorized(new { message = "Invalid authorization token." });
    //        }

    //        await _authService.LogoutAsync(accessToken);
    //        return Ok(new { message = "Logged out successfully." });
    //    }
    //    catch (UnauthorizedAccessException)
    //    {
    //        return Unauthorized(new { message = "Invalid token." });
    //    }
    //    catch (Exception ex)
    //    {
    //        Console.WriteLine($"[AuthController.Logout] Error: {ex.Message}");
    //        return StatusCode(500, new { message = $"Logout failed: {ex.Message}" });
    //    }
    //}
}
