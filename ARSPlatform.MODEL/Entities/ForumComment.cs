using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ARSPlatform.MODEL.Entities;

public partial class ForumComment
{
    public int ForumCommentId { get; set; }

    public int? UserId { get; set; }

    public int? PaperId { get; set; }

    public int? ForumPostId { get; set; }

    public string Content { get; set; } = null!;

    public int? ReplyId { get; set; }

    public int? UpvoteCount { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [JsonIgnore]
    public virtual ICollection<CommentVote> CommentVotes { get; set; }
        = new List<CommentVote>();

    public virtual ICollection<ForumComment> InverseReply { get; set; }
        = new List<ForumComment>();

    public virtual Paper? Paper { get; set; }

    [JsonIgnore]
    public virtual ForumPost? ForumPost { get; set; }

    [JsonIgnore]
    public virtual ForumComment? Reply { get; set; }

    public virtual User? User { get; set; }
}