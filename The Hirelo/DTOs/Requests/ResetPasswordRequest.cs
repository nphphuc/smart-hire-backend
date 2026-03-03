namespace The_Hirelo.DTOs.Requests
{
    public class ResetPasswordRequest
    {
        public string Email { get; set; } = null!;
        public string ConfirmationCode { get; set; } = null!;
        public string NewPassword { get; set; } = null!;
    }
}
