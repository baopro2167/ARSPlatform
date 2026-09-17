using System;

namespace ARSPlatform.SERVICE.DTOs.Response
{
    /// <summary>
    /// Response trả về thông tin 1 UserReward.
    /// </summary>
    public class UserRewardResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public int RewardMonths { get; set; }
        public DateTime UpdateAt { get; set; }
        public string Status { get; set; } = "Active";
        public DateTime CreatedAt { get; set; }
    }
}
