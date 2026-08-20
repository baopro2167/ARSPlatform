using System.Collections.Generic;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class GradingRubricCriterionResponse
    {
        public string Code { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int MaxScore { get; set; }

        public int Order { get; set; }

        public List<string> StandardReferences { get; set; } = new List<string>();
    }
}