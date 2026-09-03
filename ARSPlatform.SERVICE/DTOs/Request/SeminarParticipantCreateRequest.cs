namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class SeminarParticipantCreateRequest
    {
        public int? SeminarId { get; set; }
        public int? UserId { get; set; }
        public string? InvitedEmail { get; set; }
        public string? InvitationStatus { get; set; }

        // Hỗ trợ structured feedback mới nếu luồng cũ tạo participant kèm feedback.
        public SeminarFeedbackContentRequest? Feedback { get; set; }

        // Giữ tương thích request cũ; được chuyển thành Feedback.OverallComment.
        public string? ParticipantEvaluation { get; set; }
    }
}