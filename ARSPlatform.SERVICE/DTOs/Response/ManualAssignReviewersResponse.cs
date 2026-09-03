using System;
using System.Collections.Generic;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class ManualAssignReviewersResponse
    {
        public int PaperId { get; set; }

        public string PaperTitle { get; set; } = string.Empty;

        public int RequestedCount { get; set; }

        public int AssignedCount { get; set; }

        public List<AssignedReviewerDto> AssignedReviewers { get; set; } = new List<AssignedReviewerDto>();

        public List<string> Warnings { get; set; } = new List<string>();

        public string Message { get; set; } = string.Empty;
    }
}
