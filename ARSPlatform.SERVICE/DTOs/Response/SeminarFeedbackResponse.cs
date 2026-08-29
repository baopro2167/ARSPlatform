namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class SeminarFeedbackResponse
    {
        public int SeminarId { get; set; }

        public int SeminarParticipantId { get; set; }

        public string ParticipantEvaluation { get; set; } = string.Empty;

        public string InvitationStatus { get; set; } = "SUBMITTED";

        public string Message { get; set; } = "Feedback submitted successfully.";
    }
}
