using System;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class PhasedReportCreateRequest
    {
        public int? ResearchGroupId { get; set; }

        public int? GroupMemberId { get; set; }

        public string? ReportFileUrl { get; set; }

        public string? CapacityEvaluation { get; set; }

        public string? FinalOutcomeEvaluation { get; set; }

        public decimal? LectureFeedback { get; set; }

        public int? PhaseNumber { get; set; }

        public string? MilestoneTitle { get; set; }

        public string? Status { get; set; }

        public DateTime? SubmittedAt { get; set; }
    }
}
