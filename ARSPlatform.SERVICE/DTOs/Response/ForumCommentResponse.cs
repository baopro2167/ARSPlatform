using System;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class ForumCommentResponse
    {
        public int ForumCommentId { get; set; }

        public int? UserId { get; set; }

        public int? PaperId { get; set; }

        public int? ForumPostId { get; set; }

        public string Content { get; set; } = string.Empty;

        public int? ReplyId { get; set; }

        public int? UpvoteCount { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public string? Author { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string? AuthorAvatar { get; set; }

        public bool IsUpvoted { get; set; } = false;
    }
}