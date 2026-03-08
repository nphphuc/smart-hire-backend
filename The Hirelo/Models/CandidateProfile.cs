using System;
using System.Collections.Generic;

namespace The_Hirelo.Models;

public partial class CandidateProfile
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string? Seniority { get; set; }

    public string? FileUrl { get; set; }

    public string? FileKey { get; set; }

    public Guid? JobId { get; set; }

    public string? ParsedSkillsJson { get; set; }

    public string? Strengths { get; set; }

    public string? Gaps { get; set; }

    public double? MatchingScore { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<InterviewSession> InterviewSessions { get; set; } = new List<InterviewSession>();

    public virtual Job? Job { get; set; }

    public virtual User User { get; set; } = null!;
}
