namespace The_Hirelo.DTOs.Responses
{
    public class EmotionTimelineResponse
    {
        public DateTime Timestamp { get; set; }
        public string? Emotion { get; set; }
        public double Confidence { get; set; }
    }
}