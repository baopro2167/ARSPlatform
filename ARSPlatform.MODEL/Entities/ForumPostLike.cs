using System;
using System.Text.Json.Serialization;

namespace ARSPlatform.MODEL.Entities;

public partial class ForumPostLike
{
    public int UserId { get; set; }

    public int ForumPostId { get; set; }

    public DateTime? CreatedAt { get; set; }

    [JsonIgnore]
    public virtual ForumPost ForumPost { get; set; } = null!;

    [JsonIgnore]
    public virtual User User { get; set; } = null!;
}
