using System;
using System.Collections.Generic;

namespace The_Hirelo.Models;

public partial class InterviewSession
{
    public Guid Id { get; set; }

    public Guid JobId { get; set; }

    public Guid CandidateId { get; set; }

    public string? Status { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? EndedAt { get; set; }

    public virtual CandidateProfile Candidate { get; set; } = null!;

    public virtual ICollection<CodeSubmission> CodeSubmissions { get; set; } = new List<CodeSubmission>();

    public virtual ICollection<EmotionFrame> EmotionFrames { get; set; } = new List<EmotionFrame>();

    public virtual ICollection<InterviewQuestion> InterviewQuestions { get; set; } = new List<InterviewQuestion>();

    public virtual InterviewReport? InterviewReport { get; set; }

    public virtual Job Job { get; set; } = null!;

    public virtual Scorecard? Scorecard { get; set; }
}
