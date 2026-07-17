using System;
using System.Collections.Generic;

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

    public virtual ICollection<ResearchGroup> ResearchGroups { get; set; } = new List<ResearchGroup>();
}
