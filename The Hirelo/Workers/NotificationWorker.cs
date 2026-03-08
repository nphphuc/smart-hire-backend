using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using The_Hirelo.Services;

namespace The_Hirelo.Workers
{
    /// <summary>
    /// CONSUME cv_parsed_done
    ///   → sendEmail(…) via SES
    ///   → push event_cv_ready via WebSocket to Recruiter Dashboard
    /// </summary>
    public class NotificationWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IAmazonSQS _sqs;
        private readonly IConfiguration _config;
        private readonly ILogger<NotificationWorker> _logger;

        public NotificationWorker(
            IServiceScopeFactory scopeFactory,
            IAmazonSQS sqs,
            IConfiguration config,
            ILogger<NotificationWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _sqs = sqs;
            _config = config;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var queueUrl = _config["AWS:SQS:CVParsedDoneQueueUrl"]!;
            _logger.LogInformation("NotificationWorker started, polling: {Queue}", queueUrl);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var response = await _sqs.ReceiveMessageAsync(new ReceiveMessageRequest
                    {
                        QueueUrl = queueUrl,
                        MaxNumberOfMessages = 5,
                        WaitTimeSeconds = 10
                    }, stoppingToken);

                    foreach (var msg in response.Messages ?? [])
                        await ProcessAsync(msg, queueUrl, stoppingToken);
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogError(ex, "NotificationWorker polling error");
                    await Task.Delay(5_000, stoppingToken);
                }
            }
        }

        private async Task ProcessAsync(Message sqsMsg, string queueUrl, CancellationToken ct)
        {
            try
            {
                var msg = JsonSerializer.Deserialize<CVParsedDoneMessage>(sqsMsg.Body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

                using var scope = _scopeFactory.CreateScope();
                var notifService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                // sendEmail(…) via SES to recruiter/candidate
                if (!string.IsNullOrEmpty(msg.CandidateEmail))
                {
                    await notifService.SendCVReadyEmailAsync(
                        toEmail: msg.CandidateEmail,
                        candidateName: msg.CandidateName ?? "Candidate",
                        matchingScore: msg.MatchingScore ?? 0,
                        profileId: Guid.Parse(msg.ProfileId));
                }

                // push event_cv_ready via WebSocket → Recruiter Dashboard
                if (!string.IsNullOrEmpty(msg.RecruiterId))
                {
                    await notifService.PushCVReadyEventAsync(
                        userId: msg.RecruiterId,
                        profileId: Guid.Parse(msg.ProfileId),
                        matchingScore: msg.MatchingScore ?? 0);
                }

                await _sqs.DeleteMessageAsync(queueUrl, sqsMsg.ReceiptHandle, ct);
                _logger.LogInformation("Notification sent for ProfileId={Id}", msg.ProfileId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NotificationWorker failed | MsgId={Id}", sqsMsg.MessageId);
            }
        }

        private record CVParsedDoneMessage(
            string ProfileId,
            string? CandidateId,
            string? RecruiterId,
            string? CandidateEmail,
            string? CandidateName,
            double? MatchingScore);
    }
}
