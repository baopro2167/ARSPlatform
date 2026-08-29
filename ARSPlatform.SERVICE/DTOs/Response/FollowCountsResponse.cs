using System;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    public class FollowCountsResponse
    {
        public int UserId { get; set; }
        public int FollowersCount { get; set; }
        public int FollowingCount { get; set; }
    }
}
