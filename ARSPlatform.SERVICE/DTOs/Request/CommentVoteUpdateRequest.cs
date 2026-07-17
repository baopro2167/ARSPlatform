using System;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class CommentVoteUpdateRequest
    {
        public int UserId { get; set; }

        public int ForumCommentId { get; set; }
    }
}
