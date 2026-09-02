using System;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class SeminarFeedbackResponse
    {
        public int SeminarId { get; set; }

        public int SeminarParticipantId { get; set; }

        public int? UserId { get; set; }

        public int Rating { get; set; }

        public string ParticipantEvaluation { get; set; } = string.Empty;

        public DateTime FeedbackSubmittedAt { get; set; }

        public string InvitationStatus { get; set; } = "SUBMITTED";

        public string Message { get; set; } = "Feedback submitted successfully.";
    }
}