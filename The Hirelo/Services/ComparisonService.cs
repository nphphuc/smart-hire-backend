using The_Hirelo.Services.Interfaces;
using The_Hirelo.DTOs.Responses;

namespace The_Hirelo.Services
{
    public class ComparisonService : IComparisonService
    {
        public async Task<CandidateComparisonResponse> CompareCandidatesAsync(IEnumerable<Guid> candidateIds)
        {
            // Placeholder: return empty comparison
            await Task.CompletedTask;
            return new CandidateComparisonResponse
            {
                Candidates = new List<ComparedCandidateResponse>(),
                Metrics = new List<string>()
            };
        }
    }
}
