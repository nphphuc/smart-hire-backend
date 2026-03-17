using The_Hirelo.Models;

namespace The_Hirelo.Repositories.Interfaces
{
    public interface IJobRepository
    {
        Task<Job> CreateAsync(Job job);
        Task<Job?> GetByIdAsync(Guid id);
        Task<IEnumerable<Job>> GetByRecruiterIdAsync(Guid recruiterId);
        Task<Job> UpdateAsync(Job job);
        Task DeleteAsync(Guid id);
        Task SaveJdFileMetadataAsync(Guid jobId, string fileUrl);
    }
}
