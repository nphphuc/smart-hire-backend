namespace The_Hirelo.DTOs.Responses
{
    /// <summary>
    /// Recruiter job list (compact) and candidate catalog (includes <see cref="Description"/> when set).
    /// </summary>
    public class JobListItemResponse
    {
        public Guid Id { get; set; }
        public string? Title { get; set; }
        public string? CompanyName { get; set; }
        public DateTime? CreatedAt { get; set; }

        /// <summary>Full JD text for catalog endpoints; null for recruiter list responses.</summary>
        public string? Description { get; set; }
    }
}