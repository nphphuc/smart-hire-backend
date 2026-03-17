namespace The_Hirelo.Models;
public class Company
{
    public Guid Id { get; set; }

    public string? Name { get; set; }

    public string? TaxCode { get; set; }

    public DateTime? CreatedAt { get; set; }

    public ICollection<RecruiterProfile> RecruiterProfiles { get; set; } = new List<RecruiterProfile>();
}
