using System;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class ForumCommentUpdateRequest
    {
        public int? UserId { get; set; }

        public int? PaperId { get; set; }

        public string Content { get; set; }

        public int? ReplyId { get; set; }

        public int? UpvoteCount { get; set; }
    }
}
