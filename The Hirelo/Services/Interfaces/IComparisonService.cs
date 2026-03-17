using The_Hirelo.DTOs.Responses;

namespace The_Hirelo.Services.Interfaces
{
    public interface IComparisonService
    {
        Task<CandidateComparisonResponse> CompareCandidatesAsync(IEnumerable<Guid> candidateIds);
    }
}
