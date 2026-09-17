namespace ARSPlatform.SERVICE.DTOs.Request
{
    /// <summary>
    /// Query string cho API GET phân trang UserReward.
    /// </summary>
    public class UserRewardPaginationRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        /// <summary>
        /// Lọc theo Status chính xác (Active | InActive).
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// Tìm kiếm theo Name (contains, case-insensitive).
        /// </summary>
        public string? Search { get; set; }
    }
}
