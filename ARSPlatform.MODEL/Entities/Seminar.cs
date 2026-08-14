using System;
using System.Collections.Generic;

namespace ARSPlatform.MODEL.Entities;

public partial class Seminar
{
    public int SeminarId { get; set; }

    public int? OrganizerId { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public string Content { get; set; } = null!;

    public string? OnlineLink { get; set; }

    public int? MaxParticipants { get; set; }

    public bool? IsReminderSent { get; set; }

    public string? Status { get; set; }

    // Existing AI feature
    public string? AiSummary { get; set; }

    // Existing seminar feedback feature
    public string? Feedback { get; set; }

    public virtual User? Organizer { get; set; }

    public virtual ICollection<SeminarParticipant> SeminarParticipants { get; set; } = new List<SeminarParticipant>();
}
