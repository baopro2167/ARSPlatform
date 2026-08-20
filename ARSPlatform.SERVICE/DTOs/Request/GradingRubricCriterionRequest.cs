using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class GradingRubricCriterionRequest
    {
        [Required]
        [MaxLength(100)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Range(1, 10)]
        public int MaxScore { get; set; } = 5;

        [Range(1, int.MaxValue)]
        public int Order { get; set; }

        public List<string> StandardReferences { get; set; } = new List<string>();
    }
}