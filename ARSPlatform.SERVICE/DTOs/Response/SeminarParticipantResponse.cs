using System;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class SeminarParticipantResponse
    {
        public int SeminarParticipantId { get; set; }

        public int? SeminarId { get; set; }

        public int? UserId { get; set; }

        public string? InvitationStatus { get; set; }

        public string? ParticipantEvaluation { get; set; }
    }
