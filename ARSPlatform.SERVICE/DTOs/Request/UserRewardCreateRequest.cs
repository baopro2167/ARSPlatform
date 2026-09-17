using System.ComponentModel.DataAnnotations;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    /// <summary>
    /// Request tạo mới 1 UserReward (phần thưởng cho Researcher).
    /// </summary>
    public class UserRewardCreateRequest
    {
        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string Name { get; set; } = null!;

        [StringLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Số tháng thưởng cộng vào ExpiresAt. Phải > 0.
        /// </summary>
        [Range(1, 1200, ErrorMessage = "RewardMonths phải nằm trong khoảng [1, 1200]")]
        public int RewardMonths { get; set; }

        /// <summary>
        /// Active | InActive. Mặc định "Active".
        /// </summary>
        [StringLength(20)]
        public string? Status { get; set; }
    }
}
