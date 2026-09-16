using System;
using ARSPlatform.MODEL.Entities;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class UserMedalResponse
    {
        public long Id { get; set; }
        public int UserId { get; set; }
        public string MedalId { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public int CurrentProgress { get; set; }
        public int CriteriaThreshold { get; set; }
        public string? CriteriaUnit { get; set; }
        public bool IsUnlocked { get; set; }
        public double ProgressPercentage { get; set; }
        public DateTime? UnlockedAt { get; set; }
        public int? AwardedByAdminId { get; set; }
        public string? AwardedReason { get; set; }
        public string? CorrelationId { get; set; }

        public MedalStatus Status { get; set; } = MedalStatus.Active;

        public MedalSummaryDto? Medal { get; set; }
    }
}
