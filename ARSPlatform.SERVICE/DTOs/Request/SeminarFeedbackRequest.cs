using System.Collections.Generic;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class SeminarFeedbackRequest
    {
        /// <summary>
        /// Chuỗi JSON câu trả lời động (hoặc object JSON) từ Participant
        /// </summary>
        public object? FeedbackJson { get; set; }

        /// <summary>
        /// Danh sách câu trả lời động dạng Object List (nếu FE gửi mảng object)
        /// </summary>
        public List<SeminarFeedbackAnswerDto>? Answers { get; set; }

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