using System;
using System.Collections.Generic;

namespace ARSPlatform.MODEL.Entities;

public partial class MajorField
{
    public int MajorFieldId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<SubField> SubFields { get; set; } = new List<SubField>();
}
