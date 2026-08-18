using System;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class ForumCommentCreateRequest
    {
        public int? UserId { get; set; }

        public int? PaperId { get; set; }

        public int? ForumPostId { get; set; }

        public string Content { get; set; } = string.Empty;

        public int? ReplyId { get; set; }

        public int? UpvoteCount { get; set; }
    }
}