using System;
using System.Collections.Generic;

namespace ARSPlatform.MODEL.Entities;

public partial class GuidanceProject
{
    public int GuidanceProjectId { get; set; }

    public int? LecturerId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int? StudentId { get; set; }

    public virtual User? Lecturer { get; set; }

    public virtual User? Student { get; set; }
}
