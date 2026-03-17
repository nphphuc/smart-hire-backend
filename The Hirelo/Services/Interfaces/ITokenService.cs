using The_Hirelo.DTOs.Requests;

namespace The_Hirelo.Services.Interfaces;

public interface ITokenService
{
    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    Task<(bool Success, string AccessToken, string? IdToken, int ExpiresIn, string? TokenType)> RefreshTokenAsync(RefreshTokenRequest request);
}
