using System;
using System.Collections.Generic;

namespace ARSPlatform.MODEL.Entities;

public partial class ForumPost
{
    public int ForumPostId { get; set; }

    public int UserId { get; set; }

    public string? Title { get; set; }

    public string Content { get; set; } = null!;

    public string? Abstract { get; set; }

    public string? Category { get; set; }

    public string? Tags { get; set; }

    public string? AttachedPdfUrl { get; set; }

    public string? AttachedImageUrl { get; set; }

    public int LikeCount { get; set; }

    public int ViewCount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual User User { get; set; } = null!;

    public virtual ICollection<ForumComment> ForumComments { get; set; }
        = new List<ForumComment>();
}