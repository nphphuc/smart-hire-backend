using The_Hirelo.Enums;

namespace The_Hirelo.DTOs.Responses
{
    public class RecruiterVerificationResponse
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public string CompanyName { get; set; } = null!;

        public string CompanyTaxCode { get; set; } = null!;

        public string? RecruiterEmail { get; set; }

        public object? Images { get; set; }

        public VerificationStatus Status { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}
