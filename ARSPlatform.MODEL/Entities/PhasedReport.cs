using System;
using System.Collections.Generic;

namespace ARSPlatform.MODEL.Entities;

public partial class PhasedReport
{
    public int PhasedReportId { get; set; }

    public int? ResearchGroupId { get; set; }

    public int? GroupMemberId { get; set; }

    public string? ReportFileUrl { get; set; }

    public string? CapacityEvaluation { get; set; }

    public string? FinalOutcomeEvaluation { get; set; }

    public decimal? LectureFeedback { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual GroupMember? GroupMember { get; set; }

    public virtual ResearchGroup? ResearchGroup { get; set; }
}
