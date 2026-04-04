using The_Hirelo.DTOs.Requests;
using The_Hirelo.DTOs.Responses;
using The_Hirelo.Enums;
using The_Hirelo.Repositories.Interfaces;
using The_Hirelo.Services.Interfaces;

namespace The_Hirelo.Services
{
    public class ApplicationService : IApplicationService
    {
        private readonly IApplicationRepository _applicationRepository;
        private readonly IJobRepository _jobRepository;
        private readonly IUserRepository _userRepository;
        private readonly IApplicationTrackingService _applicationTrackingService;

        public ApplicationService(
            IApplicationRepository applicationRepository,
            IJobRepository jobRepository,
            IUserRepository userRepository,
            IApplicationTrackingService applicationTrackingService)
        {
            _applicationRepository = applicationRepository;
            _jobRepository = jobRepository;
            _userRepository = userRepository;
            _applicationTrackingService = applicationTrackingService;
        }

        public async Task<ApplicationResponse?> CreateApplicationAsync(Guid candidateId, CreateApplicationRequest request)
        {
            // Validate job exists
            var job = await _jobRepository.GetByIdAsync(request.JobId);
            if (job == null)
                throw new InvalidOperationException($"Job with ID {request.JobId} not found.");

            // Create application
            var application = await _applicationRepository.CreateApplicationAsync(request.JobId, candidateId);
            if (application == null)
                return null;

            // Sync to DynamoDB (Hot Store for realtime updates)
            try
            {
                await _applicationTrackingService.CreateApplicationTrackingAsync(
                    application.Id,
                    application.JobId,
                    application.CandidateId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to sync application to DynamoDB: {ex.Message}");
            }

            return MapToApplicationResponse(application);
        }

        public async Task<ApplicationResponse?> GetApplicationByIdAsync(Guid applicationId)
        {
            var application = await _applicationRepository.GetApplicationByIdAsync(applicationId);
            if (application == null)
                return null;

            return MapToApplicationResponse(application);
        }

        public async Task<IEnumerable<ApplicationListResponse>> GetApplicationsByJobIdAsync(Guid jobId)
        {
            var applications = await _applicationRepository.GetApplicationsByJobIdAsync(jobId);
            return applications.Select(MapToApplicationListResponse);
        }

        public async Task<IEnumerable<ApplicationResponse>> GetApplicationsByCandidateIdAsync(Guid candidateId)
        {
            var applications = await _applicationRepository.GetApplicationsByCandidateIdAsync(candidateId);
            return applications.Select(MapToApplicationResponse);
        }

        public async Task<bool> UpdateApplicationStatusAsync(Guid applicationId, ApplicationStatus status)
        {
            var result = await _applicationRepository.UpdateApplicationStatusAsync(applicationId, status);
            
            if (result)
            {
                // Sync status update to DynamoDB
                try
                {
                    await _applicationTrackingService.UpdateApplicationTrackingAsync(applicationId, status);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to update application tracking in DynamoDB: {ex.Message}");
                }
            }

            return result;
        }

        public async Task<bool> DeleteApplicationAsync(Guid applicationId)
        {
            var result = await _applicationRepository.DeleteApplicationAsync(applicationId);
            
            if (result)
            {
                // Remove from DynamoDB
                try
                {
                    await _applicationTrackingService.DeleteApplicationTrackingAsync(applicationId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to delete application tracking from DynamoDB: {ex.Message}");
                }
            }

            return result;
        }

        // Helper Methods
        private static ApplicationResponse MapToApplicationResponse(Models.Application application)
        {
            return new ApplicationResponse
            {
                ApplicationId = application.Id,
                JobId = application.JobId,
                CandidateId = application.CandidateId,
                Status = application.Status,
                MatchScore = application.MatchScore,
                AppliedAt = application.AppliedAt,
                UpdatedAt = application.UpdatedAt
            };
        }

        private static ApplicationListResponse MapToApplicationListResponse(Models.Application application)
        {
            var candidateUser = application.Candidate?.User;
            return new ApplicationListResponse
            {
                ApplicationId = application.Id,
                CandidateId = application.CandidateId,
                CandidateName = candidateUser?.Email ?? "Unknown",
                CandidateEmail = candidateUser?.Email,
                Status = application.Status,
                MatchScore = application.MatchScore,
                AppliedAt = application.AppliedAt,
                UpdatedAt = application.UpdatedAt
            };
        }
    }
}
