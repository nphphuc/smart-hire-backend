namespace The_Hirelo.DTOs.Responses
{
    /// <summary>Candidate-facing job row (aligned with frontend JobItem jobId/jobTitle).</summary>
    public class JobCatalogItemResponse
    {
        public Guid JobId { get; set; }
        public string? JobTitle { get; set; }
        public string? CompanyName { get; set; }
        public string? JdText { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
