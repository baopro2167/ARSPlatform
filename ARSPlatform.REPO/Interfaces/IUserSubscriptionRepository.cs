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
}
