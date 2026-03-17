using The_Hirelo.Enums;

namespace The_Hirelo.DTOs.Requests
{
    public interface InterviewSessionRequest
    {
        public InterviewStatus? Status { get; set; }
        public DateTime? EndedAt { get; set; }
    }
}
