using System;
using System.Linq;
using System.Threading.Tasks;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.PAGINATION;

namespace ARSPlatform.REPO.Interfaces;

/// <summary>
/// Repository cho bảng cấu hình UserRewards (phần thưởng cho Researcher).
/// </summary>
public interface IUserRewardRepository : IGenericRepository<UserReward>
{
    /// <summary>
    /// Lấy danh sách có phân trang + lọc theo Status, search theo Name (contains, case-insensitive).
    /// </summary>
    Task<PagedResult<UserReward>> GetPagedAsync(
        PaginationParams paginationParams,
        string? status = null,
        string? search = null);

    /// <summary>
    /// Tìm reward đầu tiên có Name chứa keyword (case-insensitive) và Status = "Active".
    /// Dùng khi paper publish để lấy RewardMonths cộng vào subscription.
    /// </summary>
    Task<UserReward?> FindActiveByNameContainsAsync(string keyword);

    /// <summary>
    /// Đếm số reward đang Active (cho admin dashboard).
    /// </summary>
    Task<int> CountActiveAsync();
}
