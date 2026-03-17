using The_Hirelo.DTOs.Requests;

namespace The_Hirelo.Services.Interfaces;

public interface IPasswordService
{
    /// <summary>
    /// Initiate forgot password flow
    /// </summary>
    Task<(bool Success, string Message)> ForgotPasswordAsync(ForgotPasswordRequest request);

    /// <summary>
    /// Reset password with confirmation code
    /// </summary>
    Task<(bool Success, string Message)> ResetPasswordAsync(ResetPasswordRequest request);

    /// <summary>
    /// Change password for authenticated user
    /// </summary>
    Task<(bool Success, string Message)> ChangePasswordAsync(ChangePasswordRequest request, string accessToken);
}
