using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using System.Text.Json;
using The_Hirelo.Data;
using The_Hirelo.Models;
using The_Hirelo.Repositories;
using Microsoft.EntityFrameworkCore;

namespace The_Hirelo.Services
{
    public class CVService : ICVService
    {
        private readonly IAmazonS3 _s3;
        private readonly IAmazonSQS _sqs;
        private readonly ICandidateProfileRepository _profileRepo;
        private readonly IConfiguration _config;
        private readonly ILogger<CVService> _logger;
        private readonly HireloDbContext _context;

        public CVService(
            IAmazonS3 s3,
            IAmazonSQS sqs,
            ICandidateProfileRepository profileRepo,
            IConfiguration config,
            ILogger<CVService> logger,
            HireloDbContext context)
        {
            _s3 = s3;
            _sqs = sqs;
            _profileRepo = profileRepo;
            _config = config;
            _logger = logger;
            _context = context;
        }

        // Step 1: putObject(cv_file) → S3
        public async Task<(string FileUrl, string FileKey)> UploadToS3Async(IFormFile file, Guid candidateId)
        {
            var bucket = _config["AWS:S3:BucketName"]!;
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileKey = $"cvs/{candidateId}/{Guid.NewGuid()}{ext}";

            using var stream = file.OpenReadStream();

            await _s3.PutObjectAsync(new PutObjectRequest
            {
                BucketName = bucket,
                Key = fileKey,
                InputStream = stream,
                ContentType = file.ContentType
            });

            var fileUrl = $"https://{bucket}.s3.amazonaws.com/{fileKey}";
            _logger.LogInformation("CV uploaded to S3: {FileKey}", fileKey);

            return (fileUrl, fileKey);
        }

        // Step 2: INSERT candidate_profile (status: PROCESSING)
        public async Task<CandidateProfile> CreateProfileAsync(Guid userId, Guid jobId, string fileUrl, string fileKey)
        {
            var profile = new CandidateProfile
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                JobId = jobId,
                FileUrl = fileUrl,
                FileKey = fileKey,
                Status = CandidateProfileStatus.Processing,
                CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
            };

            await _profileRepo.CreateAsync(profile);
            _logger.LogInformation("CandidateProfile created: {ProfileId} | Status: PROCESSING", profile.Id);

            return profile;
        }

        // Step 3: PUBLISH cv_parse_queue(profileId, fileKey, jobId)
        public async Task PublishCVParseQueueAsync(Guid profileId, string fileKey, Guid jobId)
        {
            var queueUrl = _config["AWS:SQS:CVParseQueueUrl"]!;
            var job = await _context.Jobs.FirstOrDefaultAsync(j => j.Id == jobId);
            var jdText = job != null ? $"Title: {job.Title}\n\n{job.Description}" : "";

            var body = JsonSerializer.Serialize(new
            {
                profileId = profileId.ToString(),
                fileKey,
                jobId = jobId.ToString(),
                jdText
            });

            await _sqs.SendMessageAsync(new SendMessageRequest
            {
                QueueUrl = queueUrl,
                MessageBody = body
            });

            _logger.LogInformation("Published cv_parse_queue | ProfileId: {ProfileId}", profileId);
        }
    }
}
