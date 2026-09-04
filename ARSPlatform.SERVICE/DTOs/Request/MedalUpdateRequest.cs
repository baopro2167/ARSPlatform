using System.Collections.Generic;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class MedalUpdateRequest
    {
        public string? Title { get; set; }
        public string? TitleVi { get; set; }
        public string? Description { get; set; }
        public string? DescriptionVi { get; set; }
        public List<string>? Roles { get; set; }
        public string? Tier { get; set; }
        public int? StageLevel { get; set; }
        public string? ImageUrl { get; set; }
        public string? CriteriaMetric { get; set; }
        public int? CriteriaThreshold { get; set; }
        public string? CriteriaUnit { get; set; }
        public bool? IsActive { get; set; }
    }
}
