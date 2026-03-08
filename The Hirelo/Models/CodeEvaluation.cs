using System;
using System.Collections.Generic;

namespace The_Hirelo.Models;

public partial class CodeEvaluation
{
    public Guid Id { get; set; }

    public Guid SubmissionId { get; set; }

    public bool Passed { get; set; }

    public int? RuntimeMs { get; set; }

    public int? MemoryKb { get; set; }

    public virtual CodeSubmission Submission { get; set; } = null!;
}
