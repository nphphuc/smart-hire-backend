using The_Hirelo.Enums;

namespace The_Hirelo.Models
{
    public class Application
    {
        public Guid Id { get; set; }

        public Guid JobId { get; set; }
        public Guid CandidateId { get; set; }

        public ApplicationStatus Status { get; set; } = ApplicationStatus.Applied;

        public double? MatchScore { get; set; }

        public DateTime? AppliedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Foreign Keys & Navigation
        public Job Job { get; set; } = null!;
        public CandidateProfile Candidate { get; set; } = null!;
    }
}
