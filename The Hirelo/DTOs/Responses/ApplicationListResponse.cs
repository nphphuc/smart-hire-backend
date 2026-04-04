using The_Hirelo.Enums;

namespace The_Hirelo.DTOs.Responses
{
    public class ApplicationListResponse
    {
        public Guid ApplicationId { get; set; }
        public Guid CandidateId { get; set; }
        public string? CandidateName { get; set; }
        public string? CandidateEmail { get; set; }
        public ApplicationStatus Status { get; set; }
        public double? MatchScore { get; set; }
        public DateTime? AppliedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
