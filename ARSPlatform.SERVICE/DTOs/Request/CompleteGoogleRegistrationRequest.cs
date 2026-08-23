using System.ComponentModel.DataAnnotations;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class CompleteGoogleRegistrationRequest
    {
        [Required]
        public string Credential { get; set; } = string.Empty; // Google ID Token

        [Required]
        [RegularExpression(@"^[+\d\s\-()]{8,20}$")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = string.Empty;

        [Required]
        [Url]
        public string PdfUrl { get; set; } = string.Empty;
    }
}
