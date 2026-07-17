using System;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class ForumCommentResponse
    {
        public int ForumCommentId { get; set; }

        public int? UserId { get; set; }

        public int? PaperId { get; set; }

        public string Content { get; set; }

        public int? ReplyId { get; set; }

        public int? UpvoteCount { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}
