using The_Hirelo.Models;

namespace The_Hirelo.Repositories.Interfaces
{
    public interface IInterviewRepository
    {
        Task<InterviewSession?> GetByCandidateIdAsync(Guid candidateId);
        Task<InterviewSession?> GetByIdAsync(Guid id);
        Task<InterviewSession> GetMediaAsync(Guid interviewId); // Value object chứa url video, transcript, emotion
    }
}
