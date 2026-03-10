using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using The_Hirelo.Models;
using The_Hirelo.Repositories;
using The_Hirelo.Services;

namespace The_Hirelo.Workers
{
    /// <summary>
    /// ASYNC PARSE PIPELINE (diagram):
    ///   CONSUME cv_parse_queue
    ///   → getObject(fileKey) S3
    ///   → detectDocumentText Textract
    ///   → extractStructured Bedrock
    ///   → matchingScope Bedrock
    ///   → UPDATE candidate_profiles (status: DONE)
    ///   → PUBLISH cv_parsed_done
    /// </summary>
    public class CVParseWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IAmazonSQS _sqs;
        private readonly IAmazonS3 _s3;
        private readonly IConfiguration _config;
        private readonly ILogger<CVParseWorker> _logger;

        public CVParseWorker(
            IServiceScopeFactory scopeFactory,
            IAmazonSQS sqs,
            IAmazonS3 s3,
            IConfiguration config,
            ILogger<CVParseWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _sqs = sqs;
            _s3 = s3;
            _config = config;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var parseQueueUrl = _config["AWS:SQS:CVParseQueueUrl"]!;
            _logger.LogInformation("CVParseWorker started, polling: {Queue}", parseQueueUrl);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var response = await _sqs.ReceiveMessageAsync(new ReceiveMessageRequest
                    {
                        QueueUrl = parseQueueUrl,
                        MaxNumberOfMessages = 5,
                        WaitTimeSeconds = 10,
                        VisibilityTimeout = 120
                    }, stoppingToken);

                    foreach (var msg in response.Messages ?? [])
                        await ProcessAsync(msg, parseQueueUrl, stoppingToken);
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogError(ex, "CVParseWorker polling error");
                    await Task.Delay(5_000, stoppingToken);
                }
            }
        }

        private async Task ProcessAsync(Message sqsMsg, string parseQueueUrl, CancellationToken ct)
        {
            string? profileIdStr = null;

            try
            {
                var body = JsonSerializer.Deserialize<CVParseMessage>(sqsMsg.Body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

                profileIdStr = body.ProfileId;
                var profileId = Guid.Parse(body.ProfileId);
                var jobId = Guid.Parse(body.JobId);
                var bucket = _config["AWS:S3:BucketName"]!;
                var doneQueueUrl = _config["AWS:SQS:CVParsedDoneQueueUrl"]!;

                using var scope = _scopeFactory.CreateScope();
                var profileRepo = scope.ServiceProvider.GetRequiredService<ICandidateProfileRepository>();
                var parseService = scope.ServiceProvider.GetRequiredService<ICVParseService>();

                // Load profile
                var profile = await profileRepo.GetByIdAsync(profileId)
                    ?? throw new InvalidOperationException($"Profile {profileId} not found");

                // getObject(fileKey) → raw file bytes
                var rawText = await parseService.ExtractRawTextAsync(bucket, profile.FileKey!);

                // extractStructured → Bedrock
                var structuredCV = await parseService.ExtractStructuredAsync(rawText);

                // matchingScope → Bedrock (MATCH CV vs JD)
                var matchResult = await parseService.MatchCVWithJDAsync(structuredCV, jobId);

                // UPDATE candidate_profiles (parsed_skills, strengths, gaps, status: DONE)
                profile.Seniority = structuredCV.SeniorityEstimate;
                profile.ParsedSkillsJson = JsonSerializer.Serialize(new
                {
                    frontend = structuredCV.FrontendSkills,
                    backend = structuredCV.BackendSkills,
                    devops = structuredCV.DevopsSkills,
                    soft = structuredCV.SoftSkills
                });
                profile.Strengths = matchResult.Strengths;
                profile.Gaps = matchResult.Gaps;
                profile.MatchingScore = matchResult.MatchingScore;
                profile.Status = CandidateProfileStatus.Done;

                await profileRepo.UpdateAsync(profile);
                _logger.LogInformation("Profile {Id} → DONE, score={Score}", profileId, matchResult.MatchingScore);

                // PUBLISH cv_parsed_done(profileId, candidateId, recruiterId)
                var recruiterUserId = profile.Job?.Recruiter?.UserId.ToString() ?? "";
                await _sqs.SendMessageAsync(new SendMessageRequest
                {
                    QueueUrl = doneQueueUrl,
                    MessageBody = JsonSerializer.Serialize(new
                    {
                        profileId = profile.Id.ToString(),
                        candidateId = profile.UserId.ToString(),
                        recruiterId = recruiterUserId,
                        candidateEmail = profile.User?.Email ?? "",
                        candidateName = profile.User?.Email?.Split('@')[0] ?? "Candidate",
                        matchingScore = matchResult.MatchingScore
                    })
                });

                // Delete processed SQS message
                await _sqs.DeleteMessageAsync(parseQueueUrl, sqsMsg.ReceiptHandle, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CVParseWorker failed | ProfileId={Id}", profileIdStr ?? "unknown");

                // Mark profile FAILED
                if (Guid.TryParse(profileIdStr, out var failedId))
                {
                    try
                    {
                        using var scope2 = _scopeFactory.CreateScope();
                        var repo = scope2.ServiceProvider.GetRequiredService<ICandidateProfileRepository>();
                        var p = await repo.GetByIdAsync(failedId);
                        if (p != null) { p.Status = CandidateProfileStatus.Failed; await repo.UpdateAsync(p); }
                    }
                    catch { /* best effort */ }
                }
            }
        }

        private record CVParseMessage(string ProfileId, string FileKey, string JobId);
    }
}
