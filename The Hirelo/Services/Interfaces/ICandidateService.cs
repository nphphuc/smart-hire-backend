using Microsoft.AspNetCore.Routing.Matching;
using The_Hirelo.DTOs.Responses;
using The_Hirelo.Enums;

namespace The_Hirelo.Services.Interfaces
{
    public interface ICandidateService
    {
        Task<IEnumerable<CandidateListItemResponse>> GetCandidatesByJobAsync(Guid jobId);
        Task<CandidateDetailResponse> GetCandidateDetailsAsync(Guid candidateId);
        Task UpdateCandidateStatusAsync(Guid candidateId, ApplicationStatus status);
    }
}
