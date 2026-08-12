using System;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class SeminarParticipantCreateRequest
    {
        public int? SeminarId { get; set; }

        public Guid? UserId { get; set; }

        public string? InvitationStatus { get; set; }
    }
}