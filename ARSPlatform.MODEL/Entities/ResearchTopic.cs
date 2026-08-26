using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ARSPlatform.MODEL.Entities;

public partial class ResearchTopic
{
    public int TopicId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? MaterialsUrl { get; set; }

    [JsonIgnore]
    public virtual ICollection<ResearchGroup> ResearchGroups { get; set; } = new List<ResearchGroup>();
}
