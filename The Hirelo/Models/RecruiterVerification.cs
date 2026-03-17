using The_Hirelo.Enums;

namespace The_Hirelo.Models
{
    public class RecruiterVerification
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public string CompanyName { get; set; } = null!;

        public string CompanyTaxCode { get; set; } = null!;

        public string? RecruiterEmail { get; set; }

        public string ImagesJson { get; set; } = null!;

        public VerificationStatus Status { get; set; } = VerificationStatus.Pending;

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        // Navigation
        public User User { get; set; } = null!;
    }
}
