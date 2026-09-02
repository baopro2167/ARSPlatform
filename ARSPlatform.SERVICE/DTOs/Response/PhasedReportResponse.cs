using System;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class PhasedReportResponse
    {
        public int PhasedReportId { get; set; }

        public int? ResearchGroupId { get; set; }

        public int? TopicId { get; set; }

        public string? TopicTitle { get; set; }

        public int? GroupMemberId { get; set; }

        public string? ReportFileUrl { get; set; }

        public string? CapacityEvaluation { get; set; }

        public string? FinalOutcomeEvaluation { get; set; }

        public decimal? LectureFeedback { get; set; }

        public string? LecturerDescription { get; set; }

        public int? PhaseNumber { get; set; }

        public string? MilestoneTitle { get; set; }

        public string? Status { get; set; }

        public string? PhasedMaterialsUrl { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? DeadlineAt { get; set; }

        public DateTime? SubmittedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public string? GroupName { get; set; }

        public string? StudentName { get; set; }

        public bool IsOverdue => DeadlineAt.HasValue && SubmittedAt.HasValue && SubmittedAt.Value > DeadlineAt.Value;
    }
}
