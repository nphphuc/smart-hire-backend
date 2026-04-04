using The_Hirelo.Enums;
using The_Hirelo.Models;

namespace The_Hirelo.Repositories.Interfaces
{
    public interface IApplicationRepository
    {
        Task<Application?> CreateApplicationAsync(Guid jobId, Guid candidateId);
        Task<Application?> GetApplicationByIdAsync(Guid applicationId);
        Task<IEnumerable<Application>> GetApplicationsByJobIdAsync(Guid jobId);
        Task<IEnumerable<Application>> GetApplicationsByCandidateIdAsync(Guid candidateId);
        Task<Application?> GetApplicationAsync(Guid jobId, Guid candidateId);
        Task<bool> ApplicationExistsAsync(Guid jobId, Guid candidateId);
        Task<bool> UpdateApplicationStatusAsync(Guid applicationId, ApplicationStatus status);
        Task<bool> DeleteApplicationAsync(Guid applicationId);
    }
}
