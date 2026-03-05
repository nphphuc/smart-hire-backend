namespace The_Hirelo.DTOs.Responses
{
    public class CandidateComparisonResponse
    {
        public IEnumerable<ComparedCandidateResponse> Candidates { get; set; } = new List<ComparedCandidateResponse>();
        public IEnumerable<string>? Metrics { get; set; }
    }
}
