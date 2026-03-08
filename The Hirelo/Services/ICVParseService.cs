namespace The_Hirelo.Services
{
    public interface ICVParseService
    {
        Task<string> ExtractRawTextAsync(byte[] fileBytes);
        Task<CVStructuredResult> ExtractStructuredAsync(string rawText);
        Task<CVMatchResult> MatchCVWithJDAsync(CVStructuredResult cvData, Guid jobId);
    }

    public class CVStructuredResult
    {
        public string? Seniority { get; set; }
        public List<string> Skills { get; set; } = new();
        public string? Summary { get; set; }
    }

    public class CVMatchResult
    {
        public double MatchingScore { get; set; }
        public string? Strengths { get; set; }
        public string? Gaps { get; set; }
    }
}
