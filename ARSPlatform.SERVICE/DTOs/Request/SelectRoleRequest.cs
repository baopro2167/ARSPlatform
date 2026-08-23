using System.ComponentModel.DataAnnotations;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class SelectRoleRequest
    {
        [Required]
        public string Role { get; set; } = string.Empty;
    }
}
