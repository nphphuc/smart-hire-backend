using The_Hirelo.DTOs.Requests;
using The_Hirelo.DTOs.Responses;
using The_Hirelo.Enums;

namespace The_Hirelo.Services.Interfaces
{
    public interface IApplicationService
    {
        Task<ApplicationResponse?> CreateApplicationAsync(Guid candidateId, CreateApplicationRequest request);
        Task<ApplicationResponse?> GetApplicationByIdAsync(Guid applicationId);
        Task<IEnumerable<ApplicationListResponse>> GetApplicationsByJobIdAsync(Guid jobId);
        Task<IEnumerable<ApplicationResponse>> GetApplicationsByCandidateIdAsync(Guid candidateId);
        Task<bool> UpdateApplicationStatusAsync(Guid applicationId, ApplicationStatus status);
        Task<bool> DeleteApplicationAsync(Guid applicationId);
    }
}
