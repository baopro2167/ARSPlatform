namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class MedalSummaryDto
    {
        public string Id { get; set; } = null!;
        public string Code { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string TitleVi { get; set; } = null!;
        public string? Description { get; set; }
        public string? DescriptionVi { get; set; }
        public string Tier { get; set; } = null!;
        public int StageLevel { get; set; }
        public string ImageUrl { get; set; } = null!;
        public string CriteriaMetric { get; set; } = null!;
        public int CriteriaThreshold { get; set; }
        public string CriteriaUnit { get; set; } = null!;
    }
}
