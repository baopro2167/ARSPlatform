using System.Linq.Expressions;
using System.Threading.Tasks;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.PAGINATION;

namespace ARSPlatform.REPO.Interfaces;

public interface IAnnualFeeRepository : IGenericRepository<AnnualFee>
{
    /// <summary>
    /// Lấy tất cả gói (có filter + sort + phân trang)
    /// </summary>
    Task<PagedResult<AnnualFee>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<AnnualFee, bool>>? predicate = null,
        Func<IQueryable<AnnualFee>, IOrderedQueryable<AnnualFee>>? orderBy = null);

    /// <summary>
    /// Lấy gói active theo role + cycle
    /// </summary>
    Task<AnnualFee?> GetActiveByRoleAndCycleAsync(string userRole, string billingCycle);

    /// <summary>
    /// Kiểm tra có gói active nào trùng role × cycle không (trừ gói đang update)
    /// </summary>
    Task<bool> ExistsActiveDuplicateAsync(string userRole, string billingCycle, int? excludeId = null);

    /// <summary>
    /// Kiểm tra gói đã có transaction nào chưa
    /// </summary>
    Task<bool> HasTransactionsAsync(int annualFeeId);
}
