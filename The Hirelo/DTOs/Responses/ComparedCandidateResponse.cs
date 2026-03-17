namespace The_Hirelo.DTOs.Responses
{
    public class ComparedCandidateResponse
    {
        public Guid CandidateId { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public double? OverallScore { get; set; }
        public double? TechnicalScore { get; set; }
        public double? CommunicationScore { get; set; }
        public double? ProblemSolvingScore { get; set; }
        public IDictionary<string, object>? AdditionalMetrics { get; set; }
    }
}
