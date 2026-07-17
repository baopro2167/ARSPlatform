using System;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class FollowerResponse
    {
        public int FollowerId { get; set; }

        public int FollowedId { get; set; }

        public DateTime? CreatedAt { get; set; }
    }
}
