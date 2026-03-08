using The_Hirelo.Models;

namespace The_Hirelo.Repositories.Interfaces
{
    public interface IRecruiterVerificationRepository
    {
        Task AddAsync(RecruiterVerification verification);
    }
}
