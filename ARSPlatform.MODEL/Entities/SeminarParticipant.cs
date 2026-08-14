using System;
using System.Collections.Generic;

namespace ARSPlatform.MODEL.Entities;

public partial class SeminarParticipant
{
    public int SeminarParticipantId { get; set; }

    public int? SeminarId { get; set; }

    public int? UserId { get; set; }

    public string? InvitationStatus { get; set; }

    public string? ParticipantEvaluation { get; set; }

    public virtual Seminar? Seminar { get; set; }

    public virtual User? User { get; set; }
}
