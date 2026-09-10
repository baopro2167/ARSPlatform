using System;
using System.Collections.Generic;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class UserMedalProgressDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string MedalId { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string TitleVi { get; set; } = string.Empty;
        public string Tier { get; set; } = string.Empty;
        public int StageLevel { get; set; }
        public string MetricCode { get; set; } = string.Empty;
        public int CurrentProgress { get; set; }
        public int CriteriaThreshold { get; set; }
        public string CriteriaUnit { get; set; } = string.Empty;
        public double ProgressPercentage { get; set; }
        public bool IsUnlocked { get; set; }
        public DateTime? UnlockedAt { get; set; }
        public string? Rules { get; set; }
        public string? Email { get; set; }
        public List<string> UserRoles { get; set; } = new();
        public List<string> ApplicableRoles { get; set; } = new();
        public bool IsRoleEligible { get; set; }
    }
}
