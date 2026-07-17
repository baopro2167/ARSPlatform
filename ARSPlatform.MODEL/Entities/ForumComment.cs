using System;
using System.Collections.Generic;

namespace ARSPlatform.MODEL.Entities;

public partial class ForumComment
{
    public int ForumCommentId { get; set; }

    public int? UserId { get; set; }

    public int? PaperId { get; set; }

    public string Content { get; set; } = null!;

    public int? ReplyId { get; set; }

    public int? UpvoteCount { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<CommentVote> CommentVotes { get; set; } = new List<CommentVote>();

    public virtual ICollection<ForumComment> InverseReply { get; set; } = new List<ForumComment>();

    public virtual Paper? Paper { get; set; }

    public virtual ForumComment? Reply { get; set; }

    public virtual User? User { get; set; }
}
