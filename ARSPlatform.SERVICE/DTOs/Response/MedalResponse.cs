using System;
using System.Collections.Generic;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class MedalResponse
    {
        public string Id { get; set; } = null!;
        public string Code { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string TitleVi { get; set; } = null!;
        public string? Description { get; set; }
        public string? DescriptionVi { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
        public string Tier { get; set; } = null!;
        public int StageLevel { get; set; }
        public string ImageUrl { get; set; } = null!;
        public string CriteriaMetric { get; set; } = null!;
        public int CriteriaThreshold { get; set; }
        public string CriteriaUnit { get; set; } = null!;
        public bool IsActive { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
