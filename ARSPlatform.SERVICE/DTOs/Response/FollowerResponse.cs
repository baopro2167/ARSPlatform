using System;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class FollowerResponse
    {
        public int FollowerId { get; set; }
        public string? FollowerName { get; set; }
        public string? FollowerEmail { get; set; }
        public string? FollowerAvatarUrl { get; set; }

        public int FollowedId { get; set; }
        public string? FollowedName { get; set; }
        public string? FollowedEmail { get; set; }
        public string? FollowedAvatarUrl { get; set; }

        public DateTime? CreatedAt { get; set; }
    }
}
