using System.ComponentModel.DataAnnotations;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class UserUpdateRequest
    {
        [Required]
        public string FullName { get; set; } = string.Empty;

        public string? AvatarUrl { get; set; }

        public bool? IsActive { get; set; }
    }
}
