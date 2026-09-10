using System.Collections.Generic;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class MedalAnalyticsDto
    {
        public string MedalId { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string TitleVi { get; set; } = string.Empty;
        public string Tier { get; set; } = string.Empty;
        public int StageLevel { get; set; }
        public string MetricCode { get; set; } = string.Empty;
        public int CriteriaThreshold { get; set; }
        public string CriteriaUnit { get; set; } = string.Empty;
        public List<string> ApplicableRoles { get; set; } = new();
        public int TotalAwarded { get; set; }
        public int TotalInProgress { get; set; }
        public double AwardedPercentage { get; set; }
    }

    public class MedalAnalyticsResponse
    {
        public int TotalMedals { get; set; }
        public int TotalUsersWithMedals { get; set; }
        public int TotalMedalsUnlocked { get; set; }
        public List<MedalAnalyticsDto> Items { get; set; } = new();
    }
}
