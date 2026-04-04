namespace The_Hirelo.DTOs.Responses
{
    public class CVUploadResponse
    {
        public Guid ProfileId { get; set; }
        public string Status { get; set; } = null!;
        public string Message { get; set; } = null!;
    }

    public class CandidateProfileResponse
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid? JobId { get; set; }
        public string? FileUrl { get; set; }
        public string? Seniority { get; set; }
        public string? ParsedSkillsJson { get; set; }
        public string? Strengths { get; set; }
        public string? Gaps { get; set; }
        public double? MatchingScore { get; set; }
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}