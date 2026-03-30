using The_Hirelo.DTOs.Requests;
using The_Hirelo.DTOs.Responses;
using Microsoft.AspNetCore.Http;

namespace The_Hirelo.Services.Interfaces
{
    public interface IJobService
    {
        Task<JobResponse> CreateJobAsync(Guid recruiterId, CreateJobRequest dto, IFormFile? jdFile);
        Task<JobResponse> UpdateJobAsync(Guid jobId, UpdateJobRequest dto, IFormFile? jdFile);
        Task<JobDetailResponse> GetJobByIdAsync(Guid jobId);
        Task<IEnumerable<JobListItemResponse>> GetAllJobsAsync(Guid? recruiterId = null);
        Task DeleteJobAsync(Guid jobId);
        Task UploadJobDescriptionAsync(Guid jobId, IFormFile jdFile);
        Task DeleteJobDescriptionAsync(Guid jobId);
        Task TriggerJdProcessingAsync(Guid jobId);
    }
}
