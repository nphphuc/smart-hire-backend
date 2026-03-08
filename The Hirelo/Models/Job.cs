using System;
using System.Collections.Generic;

namespace The_Hirelo.Models;

public partial class Job
{
    public Guid Id { get; set; }

    public Guid RecruiterId { get; set; }

    public string? Title { get; set; }

    public string? Description { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<CandidateProfile> CandidateProfiles { get; set; } = new List<CandidateProfile>();

    public virtual ICollection<InterviewSession> InterviewSessions { get; set; } = new List<InterviewSession>();

    public virtual RecruiterProfile Recruiter { get; set; } = null!;
}
