using System.ComponentModel.DataAnnotations;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    /// <summary>
    /// Request cập nhật UserReward.
    /// </summary>
    public class UserRewardUpdateRequest
    {
        [StringLength(200, MinimumLength = 1)]
        public string? Name { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Số tháng thưởng cộng vào ExpiresAt. Phải > 0.
        /// </summary>
        [Range(1, 1200, ErrorMessage = "RewardMonths phải nằm trong khoảng [1, 1200]")]
        public int? RewardMonths { get; set; }

        /// <summary>
        /// Active | InActive.
        /// </summary>
        [StringLength(20)]
        public string? Status { get; set; }
    }
}
