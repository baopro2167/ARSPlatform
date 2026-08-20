using System.Collections.Generic;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class SpecializedEvaluationItemResponse
    {
        public string CriterionCode { get; set; } = string.Empty;

        public string CriterionTitle { get; set; } = string.Empty;

        public int MaxScore { get; set; }

        public int Score { get; set; }

        public string? Notes { get; set; }

        public List<string> StandardReferences { get; set; } = new List<string>();
    }
}