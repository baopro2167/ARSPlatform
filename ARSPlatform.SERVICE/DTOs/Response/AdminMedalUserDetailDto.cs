using System;
using System.Collections.Generic;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class MedalUserDetailDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public List<string> Roles { get; set; } = new();
        public int CurrentProgress { get; set; }
        public int CriteriaThreshold { get; set; }
        public string CriteriaUnit { get; set; } = string.Empty;
        public bool IsUnlocked { get; set; }
        public DateTime? UnlockedAt { get; set; }
        public DateTime? AwardedAt { get; set; }
        public int? AwardedByAdminId { get; set; }
        public string? AwardedReason { get; set; }
    }
}
