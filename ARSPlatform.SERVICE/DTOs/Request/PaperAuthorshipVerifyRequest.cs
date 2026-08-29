using System.ComponentModel.DataAnnotations;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class PaperAuthorshipVerifyRequest
    {
        [MaxLength(50)]
        [RegularExpression(@"^W[0-9]+$", ErrorMessage = "OpenAlexWorkId must be a canonical W-prefixed OpenAlex Work ID.")]
        public string? OpenAlexWorkId { get; set; }
    }
}