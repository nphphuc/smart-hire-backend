namespace The_Hirelo.DTOs.Requests
{
    public class RecruiterVerificationRequest
    {
        public string CompanyName { get; set; } = null!;

        public string CompanyTaxCode { get; set; } = null!;

        public string? RecruiterEmail { get; set; }

        public IFormFile CccdFront { get; set; } = null!;

        public IFormFile CccdBack { get; set; } = null!;

        public IFormFile EmployeeCard { get; set; } = null!;

        public IFormFile FacePhoto { get; set; } = null!;
    }
}
