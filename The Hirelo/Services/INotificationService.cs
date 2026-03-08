namespace The_Hirelo.Services
{
    public interface INotificationService
    {
        Task SendCVReadyEmailAsync(string toEmail, string candidateName, double matchingScore, Guid profileId);
        Task PushCVReadyEventAsync(string userId, Guid profileId, double matchingScore);
    }
}
