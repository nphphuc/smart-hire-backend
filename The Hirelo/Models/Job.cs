namespace The_Hirelo.Models
{
    public class Job
    {
        public Guid Id { get; set; }

        public Guid RecruiterId { get; set; }

        public string? Title { get; set; }
        public string? Description { get; set; }

        public DateTime? CreatedAt { get; set; }

        public RecruiterProfile Recruiter { get; set; } = null!;

        public ICollection<InterviewSession> InterviewSessions { get; set; } = new List<InterviewSession>();

        // URL/path to uploaded job description file
        public string? JdFileUrl { get; set; }
    }
}
