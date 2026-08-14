using System;
using System.Collections.Generic;

namespace ARSPlatform.MODEL.Entities;

public partial class GroupMember
{
    public int GroupMemberId { get; set; }

    public int? ResearchGroupId { get; set; }

    public int? StudentId { get; set; }

    public string? ActivityStatus { get; set; }

    public DateTime? JoinedAt { get; set; }

    public virtual ICollection<PhasedReport> PhasedReports { get; set; } = new List<PhasedReport>();

    public virtual ResearchGroup? ResearchGroup { get; set; }

    public virtual User? Student { get; set; }
}
