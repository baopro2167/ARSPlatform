using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ARSPlatform.MODEL.Entities;

public partial class Medal
{
    public string Id { get; set; } = null!;

    public string Code { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string TitleVi { get; set; } = null!;

    public string? Description { get; set; }

    public string? DescriptionVi { get; set; }

    public string Roles { get; set; } = "[\"All\"]";

    public string Tier { get; set; } = null!;

    public int StageLevel { get; set; } = 1;

    public string ImageUrl { get; set; } = null!;

    public string CriteriaMetric { get; set; } = null!;

    public int CriteriaThreshold { get; set; }

    public string CriteriaUnit { get; set; } = null!;

    public bool IsActive { get; set; } = true;

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [JsonIgnore]
    public virtual ICollection<UserMedal> UserMedals { get; set; } = new List<UserMedal>();
}
