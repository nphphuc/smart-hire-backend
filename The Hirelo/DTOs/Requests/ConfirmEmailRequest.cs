namespace The_Hirelo.DTOs.Requests
{
    public class ConfirmEmailRequest
    {
        public string Email { get; set; } = null!;
        public string ConfirmationCode { get; set; } = null!;
    }
}
