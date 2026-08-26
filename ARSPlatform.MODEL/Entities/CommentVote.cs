using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ARSPlatform.MODEL.Entities;

public partial class CommentVote
{
    public int UserId { get; set; }

    public int ForumCommentId { get; set; }

    public DateTime? CreatedAt { get; set; }

    [JsonIgnore]
    public virtual ForumComment ForumComment { get; set; } = null!;

    [JsonIgnore]
    public virtual User User { get; set; } = null!;
}
