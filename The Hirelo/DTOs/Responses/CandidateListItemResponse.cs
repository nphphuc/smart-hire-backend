namespace The_Hirelo.DTOs.Responses
{
    public class CandidateListItemResponse
    {
        public Guid CandidateId { get; set; }
        public string? Email { get; set; }
        public string? Seniority { get; set; }
        public string? LatestStatus { get; set; }
        public DateTime? LastInterviewAt { get; set; }
    }
}