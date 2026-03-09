using The_Hirelo.Models;

namespace The_Hirelo.Repositories.Interfaces
{
    public interface IRecruiterVerificationRepository
    {
        Task AddAsync(RecruiterVerification verification);
        Task<RecruiterVerification?> GetByIdAsync(Guid verificationId);
        Task<List<RecruiterVerification>> GetAllAsync();
        Task<RecruiterVerification?> GetByUserIdAsync(Guid userId);
        Task UpdateAsync(RecruiterVerification verification);
    }
}
