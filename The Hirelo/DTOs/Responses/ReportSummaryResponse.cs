namespace The_Hirelo.DTOs.Responses
{
    public class ReportSummaryResponse
    {
        public Guid ReportId { get; set; }
        public Guid InterviewId { get; set; }
        public Guid CandidateId { get; set; }
        public double OverallScore { get; set; }
        public string? SummaryHtml { get; set; }
        public string? SummaryJson { get; set; }
        public DateTime GeneratedAt { get; set; }
    }
}