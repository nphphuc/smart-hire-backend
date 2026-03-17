namespace The_Hirelo.DTOs.Responses
{
    public class JobListItemResponse
    {
        public Guid Id { get; set; }
        public string? Title { get; set; }
        public string? CompanyName { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}