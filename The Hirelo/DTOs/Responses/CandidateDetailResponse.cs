namespace The_Hirelo.DTOs.Responses
{
    public class CandidateDetailResponse
    {
        public Guid CandidateId { get; set; }
        public string? Email { get; set; }
        public string? Seniority { get; set; }
        public string? FullName { get; set; }
        public DateTime? CreatedAt { get; set; }
        public IEnumerable<JobApplicationResponse>? Applications { get; set; }
    }
}