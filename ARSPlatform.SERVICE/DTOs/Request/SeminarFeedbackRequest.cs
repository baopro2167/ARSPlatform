using System.Collections.Generic;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class SeminarFeedbackRequest
    {
        public SeminarFeedbackContentRequest? Feedback { get; set; }

        // Giữ tương thích request cũ của FE; BE sẽ chuyển giá trị này thành Feedback.OverallComment.
        public string? ParticipantEvaluation { get; set; }

        // Giữ tương thích contract cũ; flow feedback mới không phụ thuộc field này.
        public string? InvitationStatus { get; set; }
    }

    public class SeminarFeedbackContentRequest
    {
        public string? OverallComment { get; set; }
        public List<string> Strengths { get; set; } = new();
        public List<string> Improvements { get; set; } = new();
        public List<string> Suggestions { get; set; } = new();
    }
}