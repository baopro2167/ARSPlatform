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

    [JsonIgnore]
    public virtual User User { get; set; } = null!;

    public virtual Medal Medal { get; set; } = null!;
}
