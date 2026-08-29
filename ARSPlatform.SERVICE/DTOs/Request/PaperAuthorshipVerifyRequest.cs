using System.ComponentModel.DataAnnotations;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class PaperAuthorshipVerifyRequest
    {
        [Required]
        [MaxLength(100)]
        public string OpenAlexWorkId { get; set; } = string.Empty;
    }
}