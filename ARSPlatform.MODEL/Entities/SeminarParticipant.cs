using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ARSPlatform.MODEL.Entities;

public partial class SeminarParticipant
{
    public int SeminarParticipantId { get; set; }

    public int? SeminarId { get; set; }

    public int? UserId { get; set; }

    public string? InvitedEmail { get; set; }

    public string? InvitationStatus { get; set; }

    public string? FeedbackJson { get; set; }

    public DateTime? FeedbackSubmittedAt { get; set; }

    public DateTime? FeedbackUpdatedAt { get; set; }

    public DateTime? InvitationSentAt { get; set; }

    public DateTime? EventReminderSentAt { get; set; }

    public DateTime? FeedbackReminderSentAt { get; set; }

    [JsonIgnore]
    public virtual Seminar? Seminar { get; set; }

    public virtual User? User { get; set; }
}