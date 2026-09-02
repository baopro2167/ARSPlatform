using System.ComponentModel.DataAnnotations;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class SeminarFeedbackRequest
    {
        [Required]
        [MaxLength(255, ErrorMessage = "Nội dung feedback không được vượt quá 255 ký tự.")]
        public string ParticipantEvaluation { get; set; } = string.Empty;

        [Required]
        [Range(1, 10, ErrorMessage = "Rating phải nằm trong khoảng từ 1 đến 10.")]
        public int? Rating { get; set; }

        public string? InvitationStatus { get; set; }
    }
}