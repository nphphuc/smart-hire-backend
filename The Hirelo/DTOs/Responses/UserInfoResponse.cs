using The_Hirelo.Enums;

namespace The_Hirelo.DTOs.Responses
{
    public class UserInfoResponse
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = null!;
        public UserRole Role { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}