using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.REPO.PAGINATION;
using Microsoft.EntityFrameworkCore;

namespace ARSPlatform.REPOSITORIES;

public class AnnualFeeRepository : GenericRepository<AnnualFee>, IAnnualFeeRepository
{
    public AnnualFeeRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<PagedResult<AnnualFee>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<AnnualFee, bool>>? predicate = null,
        Func<IQueryable<AnnualFee>, IOrderedQueryable<AnnualFee>>? orderBy = null)
    {
        IQueryable<AnnualFee> query = _dbSet.AsNoTracking();

        if (predicate != null)
            query = query.Where(predicate);

        var totalCount = await query.CountAsync();

        if (orderBy != null)
            query = orderBy(query);

        var page = pageNumber < 1 ? 1 : pageNumber;
        var size = pageSize < 1 ? 10 : pageSize;

        var items = await query
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();

        return new PagedResult<AnnualFee>(items, totalCount, page, size);
    }

    public async Task<AnnualFee?> GetActiveByRoleAndCycleAsync(string userRole, string billingCycle)
    {
        return await _dbSet.AsNoTracking()
            .FirstOrDefaultAsync(a =>
                a.UserRole == userRole &&
                a.BillingCycle == billingCycle &&
                a.Status == true);
    }

    public async Task<bool> ExistsActiveDuplicateAsync(string userRole, string billingCycle, int? excludeId = null)
    {
        var query = _dbSet.Where(a =>
            a.UserRole == userRole &&
            a.BillingCycle == billingCycle &&
            a.Status == true);

        if (excludeId.HasValue)
            query = query.Where(a => a.Id != excludeId.Value);

        return await query.AnyAsync();
    }

    public async Task<bool> HasTransactionsAsync(int annualFeeId)
    {
        return await _context.Transactions
            .AnyAsync(t => t.AnnualFeeId == annualFeeId);
    }
}
