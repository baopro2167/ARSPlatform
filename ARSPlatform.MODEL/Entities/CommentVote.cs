using System;
using System.Collections.Generic;

namespace ARSPlatform.MODEL.Entities;

public partial class CommentVote
{
    public int UserId { get; set; }

    public int ForumCommentId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ForumComment ForumComment { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
