using The_Hirelo.DTOs.Requests;
using The_Hirelo.Models;

namespace The_Hirelo.Services.Interfaces
{
    public interface IRecruiterVerificationService
    {
        Task<Guid> SubmitAsync(Guid userId, RecruiterVerificationRequest request);
        Task<RecruiterVerification?> GetByIdAsync(Guid verificationId);
        Task<List<RecruiterVerification>> GetAllAsync();
        Task<RecruiterVerification?> GetByUserIdAsync(Guid userId);
        Task<bool> ApproveVerificationAsync(Guid userId);
        Task<bool> RejectVerificationAsync(Guid userId);
        Task<bool> RemoveRecruiterRoleAsync(Guid userId);
    }
}
