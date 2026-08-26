using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ARSPlatform.MODEL.Entities;

public partial class Follower
{
    public int FollowerId { get; set; }

    public int FollowedId { get; set; }

    public DateTime? CreatedAt { get; set; }

    [JsonIgnore]
    public virtual User Followed { get; set; } = null!;

    [JsonIgnore]
    public virtual User FollowerNavigation { get; set; } = null!;
}
