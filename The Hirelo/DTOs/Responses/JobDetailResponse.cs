namespace The_Hirelo.DTOs.Responses
{
    public class JobDetailResponse
    {
        public Guid Id { get; set; }
        public Guid RecruiterId { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string? JdFileUrl { get; set; }
        public Guid RecruiterProfileId { get; set; }
        public string? RecruiterName { get; set; }
        public string? CompanyName { get; set; }
    }
}