namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class ForumPostLikeToggleResponse
    {
        public int PostId { get; set; }

        public int Likes { get; set; }

        public bool IsLiked { get; set; }
    }
}
