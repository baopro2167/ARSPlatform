using System;
using System.Collections.Generic;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class PhasedReportSubmitRequest
    {
        public int? PhasedReportId { get; set; }
        public int? TopicId { get; set; }
        public int? PhaseNumber { get; set; }
        public int? ResearchGroupId { get; set; }
        public string ReportFileUrl { get; set; } = string.Empty;
        public string? PhasedMaterialsUrl { get; set; }
        public int? GroupMemberId { get; set; }
    }

    public class PhasedReportEvaluationRequest
    {
        public string? LecturerDescription { get; set; }
        public decimal? LectureFeedback { get; set; }
        public string? CapacityEvaluation { get; set; }
        public string? FinalOutcomeEvaluation { get; set; }
        public string? Status { get; set; }
    }

    public class TopicMilestonesCreateRequest
    {
        public int TopicId { get; set; }
        public int? ResearchGroupId { get; set; }
        public List<TopicPhaseItem> Phases { get; set; } = new List<TopicPhaseItem>();
    }

    public class TopicPhaseItem
    {
        public int PhaseNumber { get; set; }
        public string MilestoneTitle { get; set; } = string.Empty;
        public DateTime? DeadlineAt { get; set; }
    }
}
