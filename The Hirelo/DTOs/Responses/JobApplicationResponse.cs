namespace The_Hirelo.DTOs.Responses
{
    public class JobApplicationResponse
    {
        public Guid JobId { get; set; }
        public string? JobTitle { get; set; }
        public string? CompanyName { get; set; }
        public string? Status { get; set; }
        public DateTime? AppliedAt { get; set; }
    }
}