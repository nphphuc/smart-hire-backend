namespace The_Hirelo.DTOs.Responses
{
    public class AdminUserResponse
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = null!;
        public string Role { get; set; } = null!;
        public string? CognitoSub { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AdminCandidateResponse
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Email { get; set; } = null!;
        public Guid? JobId { get; set; }
        public string? JobTitle { get; set; }
        public string? Seniority { get; set; }
        public double? MatchingScore { get; set; }
        public string Status { get; set; } = null!;
        public string? FileUrl { get; set; }
        public string? Strengths { get; set; }
        public string? Gaps { get; set; }
        public string? ParsedSkillsJson { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}