using Microsoft.AspNetCore.Mvc;
using The_Hirelo.DTOs.Requests;
using The_Hirelo.Services.Interfaces;

namespace The_Hirelo.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmailController : ControllerBase
{
    private readonly IEmailService _emailService;

    public EmailController(IEmailService emailService)
    {
        _emailService = emailService;
    }

    /// <summary>
    /// Confirm email with confirmation code
    /// </summary>
    /// <param name="request">Email and confirmation code</param>
    /// <returns>Confirmation result</returns>
    [HttpPost("confirm")]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request)
    {
        try
        {
            var (success, message) = await _emailService.ConfirmEmailAsync(request);

            if (!success)
                return BadRequest(new { message });

            return Ok(new { message });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EmailController.ConfirmEmail] Error: {ex.Message}");
            return StatusCode(500, new { message = $"Confirmation failed: {ex.Message}" });
        }
    }

    /// <summary>
    /// Resend confirmation code
    /// </summary>
    /// <param name="request">Email address</param>
    /// <returns>Resend result</returns>
    [HttpPost("resend")]
    public async Task<IActionResult> ResendConfirmation([FromBody] ResendConfirmationRequest request)
    {
        try
        {
            var (success, message) = await _emailService.ResendConfirmationAsync(request);
            return Ok(new { message });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EmailController.ResendConfirmation] Error: {ex.Message}");
            return StatusCode(500, new { message = $"Failed to resend code: {ex.Message}" });
        }
    }
}
