using System.Collections.Generic;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class MedalDropdownCategoryDto
    {
        public string Category { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string CategoryNameEn { get; set; } = string.Empty;
        public string MetricCode { get; set; } = string.Empty;
        public string? Rules { get; set; }
        public List<MedalDropdownItemDto> Medals { get; set; } = new();
    }

    public class MedalDropdownItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string TitleVi { get; set; } = string.Empty;
        public string Tier { get; set; } = string.Empty;
        public int StageLevel { get; set; }
        public int CriteriaThreshold { get; set; }
        public string CriteriaUnit { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public List<string> ApplicableRoles { get; set; } = new();
    }
}
