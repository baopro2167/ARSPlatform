using System;
using System.Text.Json.Serialization;

namespace ARSPlatform.MODEL.Entities;

public partial class UserMedal
{
    public long Id { get; set; }

    public int UserId { get; set; }

    public string MedalId { get; set; } = null!;

    public int CurrentProgress { get; set; } = 0;

    public bool IsUnlocked { get; set; } = false;

    public DateTime? UnlockedAt { get; set; }

    public DateTime? AwardedAt { get; set; }

    public int? CriteriaThreshold { get; set; }

    public string? CriteriaUnit { get; set; }

    public int? AwardedByAdminId { get; set; }

    public string? AwardedReason { get; set; }

    public string? CorrelationId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [JsonIgnore]
    public virtual User User { get; set; } = null!;

    public virtual Medal Medal { get; set; } = null!;
}
