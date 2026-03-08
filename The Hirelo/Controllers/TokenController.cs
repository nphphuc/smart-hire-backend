using Microsoft.AspNetCore.Mvc;
using The_Hirelo.DTOs.Requests;
using The_Hirelo.Services.Interfaces;

namespace The_Hirelo.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TokenController : ControllerBase
{
    private readonly ITokenService _tokenService;

    public TokenController(ITokenService tokenService)
    {
        _tokenService = tokenService;
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    /// <param name="request">Refresh token</param>
    /// <returns>New access token and ID token</returns>
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var (success, accessToken, idToken, expiresIn, tokenType) = await _tokenService.RefreshTokenAsync(request);

            if (!success)
            {
                return Unauthorized(new { message = "Invalid or expired refresh token." });
            }

            return Ok(new
            {
                accessToken,
                idToken,
                expiresIn,
                tokenType
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TokenController.RefreshToken] Error: {ex.Message}");
            return StatusCode(500, new { message = $"Token refresh failed: {ex.Message}" });
        }
    }
}
