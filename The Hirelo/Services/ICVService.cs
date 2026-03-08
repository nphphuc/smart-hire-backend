using The_Hirelo.Models;

namespace The_Hirelo.Services
{
    public interface ICVService
    {
        Task<(string FileUrl, string FileKey)> UploadToS3Async(IFormFile file, Guid candidateId);
        Task<CandidateProfile> CreateProfileAsync(Guid userId, Guid jobId, string fileUrl, string fileKey);
        Task PublishCVParseQueueAsync(Guid profileId, string fileKey, Guid jobId);
    }
}
