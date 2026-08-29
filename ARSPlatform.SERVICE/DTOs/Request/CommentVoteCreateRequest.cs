using System;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class CommentVoteCreateRequest
    {
        public int UserId { get; set; }

        public int ForumCommentId { get; set; }
    }
}
