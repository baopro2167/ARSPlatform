using System;
using System.Collections.Generic;

namespace ARSPlatform.MODEL.Entities;

public partial class Report
{
    public int ReportId { get; set; }

    public int? ReporterId { get; set; }

    public string? TargetType { get; set; }

    public int? TargetId { get; set; }

    public string Reason { get; set; } = null!;

    public string? Status { get; set; }

    public string? ViolationNotes { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User? Reporter { get; set; }
}
