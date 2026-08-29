using System;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class SeminarParticipantUpdateRequest
    {
        public string? InvitationStatus { get; set; }

        public string? ParticipantEvaluation { get; set; }
    }
}