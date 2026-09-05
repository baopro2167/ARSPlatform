using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class MedalCreateRequest
    {
        public string? Id { get; set; }
        public string? Code { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        public string? TitleVi { get; set; }

        public string? Description { get; set; }
        public string? DescriptionVi { get; set; }

        public List<string>? Roles { get; set; }

        [Required]
        public string Tier { get; set; } = "Bronze";

        public int StageLevel { get; set; } = 1;

        public string? ImageUrl { get; set; }

        public string? CriteriaMetric { get; set; }

        public int CriteriaThreshold { get; set; } = 1;

        public string? CriteriaUnit { get; set; }

        public bool? IsActive { get; set; } = true;
    }
}
