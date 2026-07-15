using System.ComponentModel.DataAnnotations;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class PaperCreateRequest
    {
        [Required]
        [MaxLength(250)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string Abstract { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Doi { get; set; }

        [MaxLength(500)]
        public string? FileUrl { get; set; }
    }
}
