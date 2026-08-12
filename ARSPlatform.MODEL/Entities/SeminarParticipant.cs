using System;

namespace ARSPlatform.MODEL.Entities
{
    public class SeminarParticipant
    {
        public int SeminarParticipantId { get; set; }

        public int? SeminarId { get; set; }

        public Guid? UserId { get; set; }

        public string? InvitationStatus { get; set; }

        public string? ParticipantEvaluation { get; set; }

        public virtual Seminar? Seminar { get; set; }

        public virtual User? User { get; set; }
    }
}