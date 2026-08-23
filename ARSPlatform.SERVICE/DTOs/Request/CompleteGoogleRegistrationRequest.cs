using System.ComponentModel.DataAnnotations;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class CompleteGoogleRegistrationRequest
    {
        [Required]
        [RegularExpression(@"^[+\d\s\-()]{8,20}$", ErrorMessage = "Invalid phone number format.")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = string.Empty;

        [Required]
        [Url(ErrorMessage = "Invalid PDF URL format.")]
        public string PdfUrl { get; set; } = string.Empty;
    }
}
