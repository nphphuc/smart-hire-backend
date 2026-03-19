using System;
using System.Collections.Generic;

namespace The_Hirelo.Models;

public partial class CodeSubmission
{
    public Guid Id { get; set; }

    public Guid SessionId { get; set; }

    public string? Language { get; set; }

    public string? SourceCode { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public virtual CodeEvaluation? CodeEvaluation { get; set; }

    public virtual InterviewSession Session { get; set; } = null!;
}
