using System;
using System.Collections.Generic;

namespace The_Hirelo.Models;

public partial class InterviewQuestion
{
    public Guid Id { get; set; }

    public Guid SessionId { get; set; }

    public string? QuestionText { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<InterviewAnswer> InterviewAnswers { get; set; } = new List<InterviewAnswer>();

    public virtual InterviewSession Session { get; set; } = null!;
}
