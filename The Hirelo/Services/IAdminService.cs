using The_Hirelo.DTOs.Responses;

namespace The_Hirelo.Services
{
    public interface IAdminService
    {
        Task<(int total, List<AdminUserResponse> data)> GetUsersAsync(string? role, int page, int pageSize);
        Task<(int total, List<AdminCandidateResponse> data)> GetCandidatesAsync(string? status, Guid? jobId, int page, int pageSize);
        Task<AdminCandidateResponse?> GetCandidateByIdAsync(Guid profileId);
        Task<bool> DeleteUserAsync(Guid userId);
        Task<bool> UpdateUserRoleAsync(Guid userId, string role);
    }
}