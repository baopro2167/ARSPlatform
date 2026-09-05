using System.ComponentModel.DataAnnotations;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class MedalDevGrantAllRequest
    {
        [Required]
        public int UserId { get; set; }

        public bool IncludePlatinum { get; set; } = true;

        public string? TierFilter { get; set; }

        public string? AwardedReason { get; set; } = "Acceptance test seeding";
    }
}
