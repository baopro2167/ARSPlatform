using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ARSPlatform.MODEL.Entities;

public partial class SubField
{
    public int SubFieldId { get; set; }

    public int? MajorFieldId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string GradingRubric { get; set; } = "[]";

    public DateTime? CreatedAt { get; set; }

    [JsonIgnore]
    public virtual ICollection<LearningMaterial> LearningMaterials { get; set; } = new List<LearningMaterial>();

    public virtual MajorField? MajorField { get; set; }

    [JsonIgnore]
    public virtual ICollection<Paper> Papers { get; set; } = new List<Paper>();

    [JsonIgnore]
    public virtual ICollection<ProfessionalProfile> ProfessionalProfiles { get; set; }
    = new List<ProfessionalProfile>();
}
