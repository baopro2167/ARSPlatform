namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class SeminarFeedbackRequest
    {
        public string ParticipantEvaluation { get; set; } = string.Empty;

        public int? Rating { get; set; }

        public string? InvitationStatus { get; set; }
    }
}
