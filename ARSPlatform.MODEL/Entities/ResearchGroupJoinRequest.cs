using System;
using System.Text.Json.Serialization;

namespace ARSPlatform.MODEL.Entities;

public partial class ResearchGroupJoinRequest
{
    public int JoinRequestId { get; set; }

    public int ResearchGroupId { get; set; }

    public int ApplicantUserId { get; set; }

    public string Status { get; set; } = "PENDING";

    public string? RejectionNote { get; set; }

    public int? DecidedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? DecidedAt { get; set; }

    [JsonIgnore]
    public virtual ResearchGroup ResearchGroup { get; set; } = null!;

    public virtual User ApplicantUser { get; set; } = null!;

    public virtual User? DecidedByUser { get; set; }
}
