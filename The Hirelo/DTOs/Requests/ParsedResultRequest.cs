namespace The_Hirelo.DTOs.Requests
{
    public class ParsedResultRequest
    {
        public Guid ProfileId { get; set; }
        public bool Success { get; set; }
        public string? SeniorityEstimate { get; set; }
        public List<string> FrontendSkills { get; set; } = new();
        public List<string> BackendSkills { get; set; } = new();
        public List<string> DevopsSkills { get; set; } = new();
        public List<string> SoftSkills { get; set; } = new();
        public int YearsExperience { get; set; }
        public double MatchingScore { get; set; }
        public string? Strengths { get; set; }
        public string? Gaps { get; set; }
    }
}