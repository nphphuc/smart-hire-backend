namespace The_Hirelo.DTOs.Responses
{
    public class ScorecardResponse
    {
        public Guid SessionId { get; set; }
        public Guid CandidateId { get; set; }
        public double TechnicalScore { get; set; }
        public double CommunicationScore { get; set; }
        public double ProblemSolvingScore { get; set; }
        public double OverallScore { get; set; }
    }
}