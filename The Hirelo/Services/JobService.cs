using The_Hirelo.Repositories.Interfaces;
using The_Hirelo.Services.Interfaces;
using The_Hirelo.DTOs.Requests;
using The_Hirelo.DTOs.Responses;
using The_Hirelo.Models;
using Microsoft.AspNetCore.Http;

namespace The_Hirelo.Services
{
    public class JobService : IJobService
    {
        private readonly IJobRepository _jobRepository;
        public JobService(IJobRepository jobRepository)
        {
            _jobRepository = jobRepository;
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
                // simple: save file to wwwroot/jd/{jobId}_{filename}
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
                CreatedAt = j.CreatedAt
            });
        }

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
            // remove file from wwwroot
            var fileName = job.JdFileUrl.TrimStart('/');
            var filePath = Path.Combine("wwwroot", fileName);
            if (File.Exists(filePath)) File.Delete(filePath);
            await _jobRepository.SaveJdFileMetadataAsync(jobId, null);
        }
    }
}
