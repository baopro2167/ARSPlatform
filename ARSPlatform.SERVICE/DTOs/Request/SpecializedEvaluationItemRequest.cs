using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class SpecializedEvaluationItemRequest
    {
        [Required]
        [MaxLength(100)]
        public string CriterionCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string CriterionTitle { get; set; } = string.Empty;

        [Range(1, 10)]
        public int MaxScore { get; set; } = 5;

        [Range(1, 10)]
        public int Score { get; set; }

        public string? Notes { get; set; }

        public List<string> StandardReferences { get; set; } = new List<string>();
    }
}