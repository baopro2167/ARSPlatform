using System;
using System.Collections.Generic;

namespace ARSPlatform.MODEL.Entities;

public partial class SharedMaterial
{
    public int SharedMaterialId { get; set; }

    public int? LecturerId { get; set; }

    public int? PaperId { get; set; }

    public int? SharedWithColleagueId { get; set; }

    public DateTime? SharedAt { get; set; }

    public string? Status { get; set; }

    public virtual User? Lecturer { get; set; }

    public virtual Paper? Paper { get; set; }

    public virtual User? SharedWithColleague { get; set; }
}
