using The_Hirelo.Enums;

namespace The_Hirelo.DTOs.Responses
{
    public class ApplicationResponse
    {
        public Guid ApplicationId { get; set; }
        public Guid JobId { get; set; }
        public Guid CandidateId { get; set; }
        public ApplicationStatus Status { get; set; }
        public double? MatchScore { get; set; }
        public DateTime? AppliedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
