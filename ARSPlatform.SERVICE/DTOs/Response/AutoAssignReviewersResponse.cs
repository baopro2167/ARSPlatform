using System;
using System.Collections.Generic;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class AutoAssignReviewersResponse
    {
        public int PaperId { get; set; }

        public string PaperTitle { get; set; } = string.Empty;

        public int? SubFieldId { get; set; }

        public int RequestedCount { get; set; }

        public int AssignedCount { get; set; }

        public List<AssignedReviewerDto> AssignedReviewers { get; set; } = new List<AssignedReviewerDto>();

        public string Message { get; set; } = string.Empty;
    }

    public class AssignedReviewerDto
    {
        public int ReviewerId { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? AvatarUrl { get; set; }

        public int? SubFieldId { get; set; }

        public decimal? ReviewFee { get; set; }

        public int ReviewRequestId { get; set; }

        public string Status { get; set; } = "PENDING";

        public DateTime CreatedAt { get; set; }
    }
}
