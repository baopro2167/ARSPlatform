namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class CommentVoteToggleResponse
    {
        public int ForumCommentId { get; set; }

        public int UpvoteCount { get; set; }

        public bool IsUpvoted { get; set; }
    }
}
