using The_Hirelo.Models;

namespace The_Hirelo.Repositories
{
    public interface ICandidateProfileRepository
    {
        Task<CandidateProfile?> GetByIdAsync(Guid profileId);
        Task<List<CandidateProfile>> GetByJobIdAsync(Guid jobId);
        Task<CandidateProfile> CreateAsync(CandidateProfile profile);
        Task UpdateAsync(CandidateProfile profile);
    }
}
