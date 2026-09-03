namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class SeminarParticipantUpdateRequest
    {
        public string? InvitationStatus { get; set; }

        // Hỗ trợ structured feedback mới cho endpoint update cũ.
        public SeminarFeedbackContentRequest? Feedback { get; set; }

        // Giữ tương thích request cũ; được chuyển thành Feedback.OverallComment.
        public string? ParticipantEvaluation { get; set; }
    }
}