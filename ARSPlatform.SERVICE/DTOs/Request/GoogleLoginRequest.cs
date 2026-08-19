using System.ComponentModel.DataAnnotations;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class GoogleLoginRequest
    {
        [Required]
        public string Credential { get; set; } = string.Empty;
    }
}
