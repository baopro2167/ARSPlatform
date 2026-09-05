using System.Collections.Generic;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class MedalDevGrantAllResponse
    {
        public int UserId { get; set; }
        public string Role { get; set; } = string.Empty;
        public int AwardedCount { get; set; }
        public int SkippedCount { get; set; }
        public List<MedalDevGrantRow> Rows { get; set; } = new();
        public string CorrelationId { get; set; } = string.Empty;
    }

    public class MedalDevGrantRow
    {
        public long Id { get; set; }
        public string MedalCode { get; set; } = string.Empty;
        public bool IsUnlocked { get; set; }
    }
}
