using System;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class UserMedalResponse
    {
        public MedalSummaryDto Medal { get; set; } = null!;
        public int CurrentProgress { get; set; }
        public bool IsUnlocked { get; set; }
        public double ProgressPercentage { get; set; }
        public DateTime? UnlockedAt { get; set; }
    }
}
