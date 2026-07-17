using System;
using System.Collections.Generic;

namespace ARSPlatform.MODEL.Entities;

public partial class ReviewRequest
{
    public int ReviewRequestId { get; set; }

    public int? PaperId { get; set; }

    public int? ReviewerId { get; set; }

    public decimal? Fee { get; set; }

    public string? Status { get; set; }

    public DateTime? Deadline { get; set; }

    public bool? Airecommended { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? Type { get; set; }

    public virtual DetailedEvaluation? DetailedEvaluation { get; set; }

    public virtual Paper? Paper { get; set; }

    public virtual User? Reviewer { get; set; }
}
