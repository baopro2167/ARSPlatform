using System;
using System.Collections.Generic;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class SubFieldResponse
    {
        public int SubFieldId { get; set; }

        public int? MajorFieldId { get; set; }

        public string? MajorFieldName { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public List<GradingRubricCriterionResponse> GradingRubric { get; set; }
            = new List<GradingRubricCriterionResponse>();

        public DateTime? CreatedAt { get; set; }
    }
}