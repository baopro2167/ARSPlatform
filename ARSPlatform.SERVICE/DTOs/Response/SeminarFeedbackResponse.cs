using System;
using System.Collections.Generic;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class SeminarFeedbackResponse
    {
        public int SeminarId { get; set; }
        public int SeminarParticipantId { get; set; }
        public int? UserId { get; set; }
        public SeminarFeedbackContentResponse Feedback { get; set; } = new();

        // Giữ tương thích response cũ; giá trị bằng Feedback.OverallComment.
        public string? ParticipantEvaluation { get; set; }

        public DateTime FeedbackSubmittedAt { get; set; }
        public DateTime? FeedbackUpdatedAt { get; set; }
        public string InvitationStatus { get; set; } = "SUBMITTED";
        public string Message { get; set; } = "Feedback submitted successfully.";
    }

    public class SeminarFeedbackContentResponse
    {
        public string? OverallComment { get; set; }
        public List<string> Strengths { get; set; } = new();
        public List<string> Improvements { get; set; } = new();
        public List<string> Suggestions { get; set; } = new();
    }
}