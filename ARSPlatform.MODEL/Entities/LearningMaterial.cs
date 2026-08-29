using System;
using System.Collections.Generic;

namespace ARSPlatform.MODEL.Entities;

public partial class LearningMaterial
{
    public int LearningMaterialId { get; set; }

    public int? LecturerId { get; set; }

    public string Title { get; set; } = null!;

    public string? FileUrl { get; set; }

    public string? Description { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int? SubFieldId { get; set; }

    public virtual User? Lecturer { get; set; }

    public virtual SubField? SubField { get; set; }
}
