namespace The_Hirelo.Models;
using System;
using System.Collections.Generic;
using The_Hirelo.Enums;

public class User
{
    public Guid Id { get; set; }

    public string CognitoSub { get; set; } = null!;

    public string Email { get; set; } = null!;

    public UserRole Role { get; set; } = UserRole.Candidate;

    public DateTime CreatedAt { get; set; }

    // Navigation
    public RecruiterProfile? RecruiterProfile { get; set; }
    public CandidateProfile? CandidateProfile { get; set; }
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
