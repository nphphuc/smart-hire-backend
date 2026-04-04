using System;
using System.Collections.Generic;

namespace The_Hirelo.Models;

public partial class InterviewReport
{
    public Guid Id { get; set; }

    public Guid SessionId { get; set; }

    public string? ReportJson { get; set; }

    public string? ReportHtml { get; set; }

    public virtual InterviewSession Session { get; set; } = null!;
}
