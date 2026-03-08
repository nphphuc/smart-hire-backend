using The_Hirelo.DTOs.Requests;

namespace The_Hirelo.Services.Interfaces;

public interface IEmailService
{
    /// <summary>
    /// Confirm email with confirmation code
    /// </summary>
    Task<(bool Success, string Message)> ConfirmEmailAsync(ConfirmEmailRequest request);

    /// <summary>
    /// Resend confirmation code
    /// </summary>
    Task<(bool Success, string Message)> ResendConfirmationAsync(ResendConfirmationRequest request);
}
