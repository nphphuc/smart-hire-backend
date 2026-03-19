namespace The_Hirelo.Services
{
    public interface ICVParseService
    {
        Task<string> ExtractRawTextAsync(string bucketName, string fileKey);
        Task<CVStructuredResult> ExtractStructuredAsync(string rawText);
        Task<CVMatchResult> MatchCVWithJDAsync(CVStructuredResult cvData, Guid jobId);
    }

    public class CVStructuredResult
    {
        public List<string> FrontendSkills { get; set; } = new();
        public List<string> BackendSkills { get; set; } = new();
        public List<string> DevopsSkills { get; set; } = new();
        public List<string> SoftSkills { get; set; } = new();
        public int YearsExperience { get; set; }
        public string? SeniorityEstimate { get; set; }
    }

    public class CVMatchResult
    {
        public double MatchingScore { get; set; }
        public string? Strengths { get; set; }
        public string? Gaps { get; set; }
    }
}