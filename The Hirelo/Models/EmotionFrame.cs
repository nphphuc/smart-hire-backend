using System;
using System.Collections.Generic;

namespace The_Hirelo.Models;

public partial class EmotionFrame
{
    public Guid Id { get; set; }

    public Guid SessionId { get; set; }

    public string? Emotion { get; set; }

    public double Confidence { get; set; }

    public DateTime? CapturedAt { get; set; }

    public virtual InterviewSession Session { get; set; } = null!;
}
