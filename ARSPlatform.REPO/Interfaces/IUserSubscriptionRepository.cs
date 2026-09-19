using System.Threading.Tasks;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.PAGINATION;

namespace ARSPlatform.REPO.Interfaces;

public interface IUserSubscriptionRepository : IGenericRepository<UserSubscription>
{
    /// <summary>
    /// Lấy subscription của user theo role
    /// </summary>
    Task<UserSubscription?> GetByUserAndRoleAsync(int userId, string userRole);

    /// <summary>
    /// Lấy subscription active (chưa hết hạn) của user theo role
    /// </summary>
    Task<UserSubscription?> GetActiveByUserAndRoleAsync(int userId, string userRole);

    /// <summary>
    /// Lấy subscription active mới nhất (nếu có nhiều)
    /// </summary>
    Task<UserSubscription?> GetLatestActiveAsync(int userId, string userRole);

    /// <summary>
    /// Kiểm tra user có subscription active theo role không
    /// </summary>
    Task<bool> IsActiveAsync(int userId, string userRole);

    /// <summary>
    /// Lấy danh sách subscribers (UserSubscription + User) của 1 gói AnnualFee, có phân trang.
    /// Join qua: UserSubscription → Transaction → AnnualFee (theo LatestTransactionId)
    /// </summary>
    Task<PagedResult<UserSubscription>> GetSubscribersByAnnualFeeIdAsync(int annualFeeId, PaginationParams paginationParams);

    /// <summary>
    /// [Admin] Lấy toàn bộ UserSubscription join User, có phân trang + filter.
    /// Hỗ trợ:
    ///   - search: tìm theo FullName / Email
    ///   - role:   lọc theo UserSubscription.UserRole
    ///   - status: Active | Expired | None
    /// Status=None sẽ LEFT JOIN để bao gồm user chưa từng đăng ký.
    /// </summary>
    Task<PagedResult<UserSubscription>> GetAllSubscriptionsPagedAsync(
        int page, int pageSize,
        string? search, string? role, string? status);
}

