using System;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class SeminarParticipantUpdateRequest
    {
        public int? SeminarId { get; set; }

        public Guid? UserId { get; set; }

        public string? InvitationStatus { get; set; }

        public string? ParticipantEvaluation { get; set; }
    }
}