namespace The_Hirelo.DTOs.Responses
{
    public class UserInfoResponse
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = null!;
        public string Role { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }
}
