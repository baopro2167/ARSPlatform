using System;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class PhasedReportUpdateRequest
    {
        public int? TopicId { get; set; }

        public int? ResearchGroupId { get; set; }

        public int? GroupMemberId { get; set; }

        public string? ReportFileUrl { get; set; }

        public string? CapacityEvaluation { get; set; }

        public string? FinalOutcomeEvaluation { get; set; }

        public decimal? LectureFeedback { get; set; }

        public string? LecturerDescription { get; set; }

        public int? PhaseNumber { get; set; }

        public string? MilestoneTitle { get; set; }
        public string? PhaseTitle
        {
            get => MilestoneTitle;
            set { if (!string.IsNullOrEmpty(value)) MilestoneTitle = value; }
        }

        public string? Requirements { get; set; }

        public string? AssessmentCriteria { get; set; }
        public string? Criteria
        {
            get => AssessmentCriteria;
            set { if (!string.IsNullOrEmpty(value)) AssessmentCriteria = value; }
        }

        public DateTime? StartDate { get; set; }
        public DateTime? StartedAt
        {
            get => StartDate;
            set { if (value.HasValue) StartDate = value; }
        }

        public string? Status { get; set; }

        public string? PhasedMaterialsUrl { get; set; }

        public DateTime? DeadlineAt { get; set; }
        public DateTime? Deadline
        {
            get => DeadlineAt;
            set { if (value.HasValue) DeadlineAt = value; }
        }

        public DateTime? SubmittedAt { get; set; }
    }
}
