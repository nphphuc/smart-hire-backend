using The_Hirelo.Models;

namespace The_Hirelo.Repositories.Interfaces
{
    public interface INotificationRepository
    {
        Task<Notification> CreateAsync(Notification notification);
        Task<IEnumerable<Notification>> GetUnreadByUserIdAsync(Guid userId);
        Task MarkAsReadAsync(Guid notificationId);
    }
}
