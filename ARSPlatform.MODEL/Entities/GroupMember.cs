using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ARSPlatform.MODEL.Entities;

public partial class GroupMember
{
    public int GroupMemberId { get; set; }

    public int? ResearchGroupId { get; set; }

    public int? StudentId { get; set; }

    public string? ActivityStatus { get; set; }

    public bool? LeaderId { get; set; }

    [JsonIgnore]
    public bool IsLeader => LeaderId == true;

    public DateTime? JoinedAt { get; set; }

    /// <summary>
    /// Ghi chú lý do Lecturer duyệt / từ chối sinh viên gia nhập nhóm.
    /// Do FE truyền xuống khi Lecturer bấm Duyệt hoặc Từ chối.
    /// </summary>
    public string? RequestNote { get; set; }

    [JsonIgnore]
    public virtual ICollection<PhasedReport> PhasedReports { get; set; } = new List<PhasedReport>();

    public virtual ResearchGroup? ResearchGroup { get; set; }

    public virtual User? Student { get; set; }
}
