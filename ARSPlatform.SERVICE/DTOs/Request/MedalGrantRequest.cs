using System.ComponentModel.DataAnnotations;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class MedalGrantRequest
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        public string MedalCode { get; set; } = string.Empty;

        public bool ForceUnlocked { get; set; } = true;

        public string? AwardedReason { get; set; }
    }
}
