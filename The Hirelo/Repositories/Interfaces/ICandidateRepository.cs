using The_Hirelo.Enums;
using The_Hirelo.Models;

namespace The_Hirelo.Repositories.Interfaces
{
    public interface ICandidateRepository
    {
        Task<IEnumerable<User>> GetCandidatesByJobIdAsync(Guid jobId);
        Task<ApplicationStatus?> GetStatusAsync(Guid candidateId);
        Task UpdateStatusAsync(Guid candidateId, ApplicationStatus status);
        Task<bool> ExistsAsync(Guid jobId, Guid candidateId);
        Task<User?> GetUserByCandidateProfileIdAsync(Guid candidateProfileId);
        Task<bool> HasCandidateAppliedToRecruiterJobAsync(Guid candidateId, Guid recruiterProfileId);
    }
}
