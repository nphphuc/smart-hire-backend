namespace The_Hirelo.Models
{
    public class RecruiterProfile
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }
        public Guid CompanyId { get; set; }

        public User User { get; set; } = null!;
        public Company Company { get; set; } = null!;

        public ICollection<Job> Jobs { get; set; } = new List<Job>();

        //Navigation
        public RecruiterVerification? Verification { get; set; }
        public bool IsVerified { get; set; }
    }
}
