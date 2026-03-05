namespace The_Hirelo.DTOs.Responses
{
    public class InterviewResultResponse
    {
        public Guid InterviewId { get; set; }
        public Guid CandidateId { get; set; }
        public string? VideoUrl { get; set; }
        public string? Transcript { get; set; }
        public IEnumerable<EmotionTimelineResponse>? Emotions { get; set; }
    }
}