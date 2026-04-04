using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using The_Hirelo.Enums;

namespace The_Hirelo.Services.Interfaces
{
    public interface IApplicationTrackingService
    {
        Task<bool> CreateApplicationTrackingAsync(Guid applicationId, Guid jobId, Guid candidateId);
        Task<bool> UpdateApplicationTrackingAsync(Guid applicationId, ApplicationStatus status);
        Task<bool> DeleteApplicationTrackingAsync(Guid applicationId);
    }
}
