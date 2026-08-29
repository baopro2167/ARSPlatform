using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ARSPlatform.MODEL.Entities;

public partial class ResearchGroup
{
    public int ResearchGroupId { get; set; }

    public int? LecturerId { get; set; }

    public int? TopicId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime? Deadline { get; set; }

    public DateTime? AssignedAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    [JsonIgnore]
    public virtual ICollection<GroupMember> GroupMembers { get; set; } = new List<GroupMember>();

    public virtual User? Lecturer { get; set; }

    [JsonIgnore]
    public virtual ICollection<PhasedReport> PhasedReports { get; set; } = new List<PhasedReport>();

    [JsonIgnore]
    public virtual ICollection<GuidanceProject> GuidanceProjects { get; set; } = new List<GuidanceProject>();

    public virtual ResearchTopic? Topic { get; set; }
}
