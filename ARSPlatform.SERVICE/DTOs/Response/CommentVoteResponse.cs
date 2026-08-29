using System;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class CommentVoteResponse
    {
        public int UserId { get; set; }

        public int ForumCommentId { get; set; }

        public DateTime? CreatedAt { get; set; }
    }
}
