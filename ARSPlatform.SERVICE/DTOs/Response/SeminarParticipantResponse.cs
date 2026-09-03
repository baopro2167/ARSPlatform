using System;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class SeminarParticipantResponse
    {
        public int SeminarParticipantId { get; set; }
        public int? SeminarId { get; set; }
        public int? UserId { get; set; }
        public string? UserFullName { get; set; }
        public string? UserEmail { get; set; }
        public string? InvitedEmail { get; set; }
        public string? InvitationStatus { get; set; }
        public SeminarFeedbackContentResponse? Feedback { get; set; }

        // Giữ tương thích response cũ; giá trị bằng Feedback.OverallComment.
        public string? ParticipantEvaluation { get; set; }

        public DateTime? FeedbackSubmittedAt { get; set; }
        public DateTime? FeedbackUpdatedAt { get; set; }
        public DateTime? InvitationSentAt { get; set; }
        public DateTime? EventReminderSentAt { get; set; }
        public DateTime? FeedbackReminderSentAt { get; set; }
    }
}