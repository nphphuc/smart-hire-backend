using The_Hirelo.DTOs.Responses;

namespace The_Hirelo.Services.Interfaces
{
    public interface IInterviewResultService
    {
        Task<InterviewResultResponse> GetInterviewResultAsync(Guid candidateId);
        Task<string> GetVideoUrlAsync(Guid interviewId);
        Task<TranscriptResponse> GetTranscriptAsync(Guid interviewId);
        Task<EmotionTimelineResponse> GetEmotionTimelineAsync(Guid interviewId);
    }
}
