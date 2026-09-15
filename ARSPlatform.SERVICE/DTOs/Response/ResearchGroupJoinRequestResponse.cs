using System;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class ResearchGroupJoinRequestResponse
    {
        public int JoinRequestId { get; set; }

        public int ResearchGroupId { get; set; }

        public string? ResearchGroupName { get; set; }

        public int ApplicantUserId { get; set; }

        public ApplicantProfileDto? Applicant { get; set; }

        public string Status { get; set; } = null!;

        public string? RejectionNote { get; set; }

        public int? DecidedByUserId { get; set; }

        public string? DecidedByUserName { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public DateTime? DecidedAt { get; set; }
    }
}
