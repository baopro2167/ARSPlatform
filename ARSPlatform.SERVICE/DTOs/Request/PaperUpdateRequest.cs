using System.ComponentModel.DataAnnotations;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class PaperUpdateRequest
    {
        [Required]
        [MaxLength(250)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string Abstract { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? FileUrl { get; set; }

        [MaxLength(30)]
        public string? Status { get; set; }

        public bool? Issn { get; set; }

        public bool? IsOpenAccess { get; set; }

        [MaxLength(50)]
        public string? Quartile { get; set; }

        public int? SubFieldId { get; set; }
    }
}
