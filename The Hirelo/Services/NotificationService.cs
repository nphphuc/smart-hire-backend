using System.Text.Json;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using The_Hirelo.Common;

namespace The_Hirelo.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IAmazonSimpleEmailService _ses;
        private readonly IWebSocketManager _wsManager;
        private readonly IConfiguration _config;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            IAmazonSimpleEmailService ses,
            IWebSocketManager wsManager,
            IConfiguration config,
            ILogger<NotificationService> logger)
        {
            _ses = ses;
            _wsManager = wsManager;
            _config = config;
            _logger = logger;
        }

        // sendEmail(…) via SES
        public async Task SendCVReadyEmailAsync(string toEmail, string candidateName, double matchingScore, Guid profileId)
        {
            var fromEmail = _config["AWS:SES:FromEmail"]!;
            var appUrl = _config["App:BaseUrl"] ?? "https://app.hirelo.io";

            var htmlBody = $"""
                <h2>CV Analysis Complete</h2>
                <p>Candidate <strong>{candidateName}</strong>'s CV has been analyzed.</p>
                <ul>
                  <li><strong>Matching Score:</strong> {matchingScore:F1} / 100</li>
                </ul>
                <p>
                  <a href="{appUrl}/candidates/{profileId}">View Full Report</a>
                </p>
                """;

            await _ses.SendEmailAsync(new SendEmailRequest
            {
                Source = fromEmail,
                Destination = new Destination { ToAddresses = [toEmail] },
                Message = new Message
                {
                    Subject = new Content($"[Hirelo] CV Ready – {candidateName} ({matchingScore:F0}% match)"),
                    Body = new Body
                    {
                        Html = new Content(htmlBody),
                        Text = new Content($"CV analysis complete. Score: {matchingScore:F1}/100. View at {appUrl}/candidates/{profileId}")
                    }
                }
            });

            _logger.LogInformation("SES email sent to {Email} for profile {ProfileId}", toEmail, profileId);
        }

        // push event_cv_ready via WebSocket to Recruiter Dashboard
        public async Task PushCVReadyEventAsync(string userId, Guid profileId, double matchingScore)
        {
            var payload = JsonSerializer.Serialize(new
            {
                @event = "cv_ready",
                profileId = profileId.ToString(),
                matchingScore,
                timestamp = DateTime.UtcNow
            });

            await _wsManager.SendToUserAsync(userId, payload);
            _logger.LogInformation("WebSocket event cv_ready pushed to user {UserId}", userId);
        }
    }
}
