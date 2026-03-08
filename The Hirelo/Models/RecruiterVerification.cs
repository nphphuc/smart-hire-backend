using The_Hirelo.Enums;

namespace The_Hirelo.Models
{
    public class RecruiterVerification
    {
        public Guid Id { get; set; }

        public Guid RecruiterProfileId { get; set; }

        public string CompanyName { get; set; } = null!;

        public string CompanyTaxCode { get; set; } = null!;

        public string? RecruiterEmail { get; set; }

        public string ImagesJson { get; set; } = null!;

        public VerificationStatus Status { get; set; } = VerificationStatus.Pending;

        public DateTime CreatedAt { get; set; }

        // Navigation
        public RecruiterProfile RecruiterProfile { get; set; } = null!;
    }
}
