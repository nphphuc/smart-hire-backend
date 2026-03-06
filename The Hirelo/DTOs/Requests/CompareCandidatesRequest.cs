namespace The_Hirelo.DTOs.Requests
{
    public class CompareCandidatesRequest
    {
        public IEnumerable<System.Guid> CandidateIds { get; set; } = new List<System.Guid>();
    }
}
