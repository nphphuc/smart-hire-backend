using The_Hirelo.DTOs.Requests;
using The_Hirelo.DTOs.Responses;

namespace The_Hirelo.Services.Interfaces;

public interface IAuthService
{
    /// <summary>
    /// Register a new user with email and password
    /// </summary>
    Task<(bool Success, string Message, string? UserSub)> RegisterAsync(RegisterRequest request);

    /// <summary>
    /// Login user with email and password
    /// </summary>
    Task<LoginResponse> LoginAsync(LoginRequest request);

    /// <summary>
    /// Logout user
    /// </summary>
    Task<bool> LogoutAsync(string accessToken);
}
