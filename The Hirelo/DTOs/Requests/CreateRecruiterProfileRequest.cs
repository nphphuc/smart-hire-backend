namespace The_Hirelo.DTOs.Requests
{
    public class CreateRecruiterProfileRequest
    {
        public string CompanyName { get; set; } = null!;
        public string? TaxCode { get; set; }
    }
}
