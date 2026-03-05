namespace The_Hirelo.DTOs.Responses
{
    public interface InterviewSessionResponse
    {
        public Guid Id { get; set; }
        public Guid JobId { get; set; }
        public Guid CandidateId { get; set; }
        public string? Status { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public string? VideoUrl { get; set; }          
        public string? TranscriptUrl { get; set; }
    }
}
