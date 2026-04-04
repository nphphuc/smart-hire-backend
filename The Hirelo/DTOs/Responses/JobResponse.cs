namespace The_Hirelo.DTOs.Responses
{
    public class JobResponse
    {
        public Guid Id { get; set; }
        public Guid RecruiterId { get; set; }
        public string? Title { get; set; }
        public string? CompanyName { get; set; }
        public string? Description { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string? JdFileUrl { get; set; }
        public string? JdText { get; set; }
    }
}