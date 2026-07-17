using System;
using System.Collections.Generic;

namespace ARSPlatform.MODEL.Entities;

public partial class Paper
{
    public int PaperId { get; set; }

    public int? CreatorId { get; set; }

    public string Title { get; set; } = null!;

    public string? Abstract { get; set; }

    public string? FileUrl { get; set; }

    public bool? Issn { get; set; }

    public bool? IsOpenAccess { get; set; }

    public string? Quartile { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? SubFieldId { get; set; }

    public virtual User? Creator { get; set; }

    public virtual ICollection<ForumComment> ForumComments { get; set; } = new List<ForumComment>();

    public virtual ICollection<ReviewRequest> ReviewRequests { get; set; } = new List<ReviewRequest>();

    public virtual ICollection<SharedMaterial> SharedMaterials { get; set; } = new List<SharedMaterial>();

    public virtual SubField? SubField { get; set; }
}
