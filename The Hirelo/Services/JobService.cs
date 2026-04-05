using The_Hirelo.Repositories.Interfaces;
using The_Hirelo.Services.Interfaces;
using The_Hirelo.DTOs.Requests;
using The_Hirelo.DTOs.Responses;
using The_Hirelo.Models;
using Microsoft.AspNetCore.Http;
using Amazon.StepFunctions;
using Amazon.StepFunctions.Model;
using System.Text.Json;

namespace The_Hirelo.Services
{
    public class JobService : IJobService
    {
        private readonly IJobRepository _jobRepository;
        private readonly IAmazonStepFunctions _stepFunctions;
        private readonly IConfiguration _configuration;
        private readonly ILogger<JobService> _logger;

        public JobService(
            IJobRepository jobRepository,
            IAmazonStepFunctions stepFunctions,
            IConfiguration configuration,
            ILogger<JobService> logger)
        {
            _jobRepository = jobRepository;
            _stepFunctions = stepFunctions;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<JobResponse> CreateJobAsync(Guid recruiterId, CreateJobRequest dto, IFormFile? jdFile)
        {
            var job = new Job
            {
                Id = Guid.NewGuid(),
                RecruiterId = recruiterId,
                Title = dto.Title,
                Description = dto.Description,
                CreatedAt = DateTime.UtcNow
            };

            if (jdFile != null)
            {
                var folder = Path.Combine("wwwroot", "jd");
                Directory.CreateDirectory(folder);
                var fileName = $"{job.Id}_{Path.GetFileName(jdFile.FileName)}";
                var filePath = Path.Combine(folder, fileName);
                using var stream = new FileStream(filePath, FileMode.Create);
                await jdFile.CopyToAsync(stream);
                var url = $"/jd/{fileName}";
                job.JdFileUrl = url;
            }

            await _jobRepository.CreateAsync(job);

            if (!string.IsNullOrWhiteSpace(dto.Description))
            {
                try 
                { 
                    await TriggerJdProcessingAsync(job.Id); 
                }
                catch (Exception ex) 
                { 
                    _logger.LogWarning(ex, "JD processing trigger failed for job {JobId}", job.Id); 
                }
            }

            return new JobResponse { Id = job.Id, Title = job.Title, Description = job.Description };
        }

        public async Task DeleteJobAsync(Guid jobId)
        {
            await _jobRepository.DeleteAsync(jobId);
        }

        public async Task<IEnumerable<JobListItemResponse>> GetAllJobsAsync(Guid? recruiterId = null)
        {
            IEnumerable<Job> jobs;
            if (recruiterId.HasValue && recruiterId != Guid.Empty)
                jobs = await _jobRepository.GetByRecruiterIdAsync(recruiterId.Value);
            else
                jobs = (await _jobRepository.GetByRecruiterIdAsync(Guid.Empty));

            return jobs.Select(j => new JobListItemResponse
            {
                Id = j.Id,
                Title = j.Title,
                CreatedAt = j.CreatedAt,
                CompanyName = j.Recruiter?.Company?.Name,
            });
        }

        public async Task<IEnumerable<JobListItemResponse>> GetCandidateJobCatalogAsync()
        {
            var jobs = await _jobRepository.GetAllForCatalogAsync();
            return jobs.Select(MapJobToListItemForCatalog);
        }

        public async Task<JobListItemResponse?> GetCandidateCatalogJobAsync(Guid jobId)
        {
            var j = await _jobRepository.GetByIdAsync(jobId);
            return j == null ? null : MapJobToListItemForCatalog(j);
        }

        private static JobListItemResponse MapJobToListItemForCatalog(Job j) =>
            new()
            {
                Id = j.Id,
                Title = j.Title,
                CompanyName = j.Recruiter?.Company?.Name,
                CreatedAt = j.CreatedAt,
                Description = j.Description,
            };

        public async Task<JobDetailResponse> GetJobByIdAsync(Guid jobId)
        {
            var j = await _jobRepository.GetByIdAsync(jobId);
            if (j == null) return null!;
            return new JobDetailResponse
            {
                Id = j.Id,
                RecruiterId = j.RecruiterId,
                Title = j.Title,
                Description = j.Description,
                CreatedAt = j.CreatedAt,
                JdFileUrl = j.JdFileUrl,
                RecruiterProfileId = j.Recruiter.Id,
                RecruiterName = j.Recruiter.User?.Email,
                CompanyName = j.Recruiter.Company?.Name
            };
        }

        public async Task<JobResponse> UpdateJobAsync(Guid jobId, UpdateJobRequest dto, IFormFile? jdFile)
        {
            var job = await _jobRepository.GetByIdAsync(jobId);
            if (job == null) return null!;
            job.Title = dto.Title ?? job.Title;
            job.Description = dto.Description ?? job.Description;

            if (jdFile != null)
            {
                var folder = Path.Combine("wwwroot", "jd");
                Directory.CreateDirectory(folder);
                var fileName = $"{job.Id}_{Path.GetFileName(jdFile.FileName)}";
                var filePath = Path.Combine(folder, fileName);
                using var stream = new FileStream(filePath, FileMode.Create);
                await jdFile.CopyToAsync(stream);
                var url = $"/jd/{fileName}";
                job.JdFileUrl = url;
            }

            await _jobRepository.UpdateAsync(job);

            if (dto.Description != null && !string.IsNullOrWhiteSpace(dto.Description))
            {
                try 
                { 
                    await TriggerJdProcessingAsync(job.Id); 
                }
                catch (Exception ex) 
                { 
                    _logger.LogWarning(ex, "JD processing trigger failed for job {JobId}", job.Id); 
                }
            }

            return new JobResponse { Id = job.Id, Title = job.Title, Description = job.Description };
        }

        public async Task UploadJobDescriptionAsync(Guid jobId, IFormFile jdFile)
        {
            var folder = Path.Combine("wwwroot", "jd");
            Directory.CreateDirectory(folder);
            var fileName = $"{jobId}_{Path.GetFileName(jdFile.FileName)}";
            var filePath = Path.Combine(folder, fileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await jdFile.CopyToAsync(stream);
            var url = $"/jd/{fileName}";
            await _jobRepository.SaveJdFileMetadataAsync(jobId, url);
        }

        public async Task DeleteJobDescriptionAsync(Guid jobId)
        {
            var job = await _jobRepository.GetByIdAsync(jobId);
            if (job == null) return;
            if (string.IsNullOrEmpty(job.JdFileUrl)) return;
            var fileName = job.JdFileUrl.TrimStart('/');
            var filePath = Path.Combine("wwwroot", fileName);
            if (File.Exists(filePath)) File.Delete(filePath);
            await _jobRepository.SaveJdFileMetadataAsync(jobId, null);
        }

        public async Task TriggerJdProcessingAsync(Guid jobId)
        {
            var stateMachineArn = Environment.GetEnvironmentVariable("STATE_MACHINE_ARN")
                ?? _configuration["AWS:StepFunctions:StateMachineArn"];

            if (string.IsNullOrEmpty(stateMachineArn))
            {
                _logger.LogWarning("STATE_MACHINE_ARN not configured; skipping JD processing for job {JobId}", jobId);
                return;
            }

            var payload = JsonSerializer.Serialize(new
            {
                profile_id = "recruiter",
                job_id = jobId.ToString()
            });

            var request = new StartExecutionRequest
            {
                StateMachineArn = stateMachineArn,
                Input = payload,
                Name = $"jd-{jobId:N}-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}"
            };

            var response = await _stepFunctions.StartExecutionAsync(request);
            _logger.LogInformation(
                "Step Functions execution started for job {JobId}: {ExecutionArn}",
                jobId, response.ExecutionArn);
        }
    }
}
