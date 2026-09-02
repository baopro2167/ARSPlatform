using System;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class SeminarInvitationResponse
    {
        public int SeminarId { get; set; }

        public int SeminarParticipantId { get; set; }

        public string Title { get; set; } = string.Empty;

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public string? OnlineLink { get; set; }

        public string? OrganizerName { get; set; }

        public string? InvitationStatus { get; set; }

        public string? ParticipantEvaluation { get; set; }

        public int? Rating { get; set; }

        public DateTime? FeedbackSubmittedAt { get; set; }
    }
}