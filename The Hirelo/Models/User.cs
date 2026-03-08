using System;
using System.Collections.Generic;

namespace The_Hirelo.Models;

public partial class User
{
    public Guid Id { get; set; }

    public string? CognitoSub { get; set; }

    public string Email { get; set; } = null!;

    public string Role { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual CandidateProfile? CandidateProfile { get; set; }

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual RecruiterProfile? RecruiterProfile { get; set; }
}
