using System;
using System.Collections.Generic;

namespace The_Hirelo.Models;

public partial class InterviewAnswer
{
    public Guid Id { get; set; }

    public Guid QuestionId { get; set; }

    public string? Transcript { get; set; }

    public string? AudioUrl { get; set; }

    public virtual InterviewQuestion Question { get; set; } = null!;
}
