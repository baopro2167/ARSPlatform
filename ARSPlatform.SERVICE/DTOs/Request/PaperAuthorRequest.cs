using System.ComponentModel.DataAnnotations;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class PaperAuthorRequest
    {
        [Required]
        [MaxLength(255)]
        public string AuthorName { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? RawAuthorName { get; set; }

        [MaxLength(19)]
        public string? OrcidId { get; set; }

        [MaxLength(100)]
        public string? OpenAlexAuthorId { get; set; }

        public bool? IsCorresponding { get; set; }
    }
}
