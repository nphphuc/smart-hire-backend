using The_Hirelo.DTOs.Requests;
using The_Hirelo.DTOs.Responses;
using The_Hirelo.Enums;

namespace The_Hirelo.Services.Interfaces;

public interface IUserService
{
    /// <summary>
    /// Get current user info from database
    /// </summary>
    Task<UserInfoResponse?> GetCurrentUserAsync(string cognitoSub);

    /// <summary>
    /// Update user role (admin only)
    /// </summary>
    Task<(bool Success, string Message)> UpdateUserRoleAsync(Guid userId, UserRole role, string? adminCognitoSub = null);

    /// <summary>
    /// Submit recruiter verification
    /// </summary>
    Task<Guid> SubmitRecruiterVerificationAsync(string cognitoSub, RecruiterVerificationRequest request, IRecruiterVerificationService recruiterVerificationService);
}
