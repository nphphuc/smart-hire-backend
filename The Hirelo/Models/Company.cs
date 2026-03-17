using System;
using System.Collections.Generic;

namespace The_Hirelo.Models;

public partial class Company
{
    public Guid Id { get; set; }

    public string? Name { get; set; }

    public string? TaxCode { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<RecruiterProfile> RecruiterProfiles { get; set; } = new List<RecruiterProfile>();
}
