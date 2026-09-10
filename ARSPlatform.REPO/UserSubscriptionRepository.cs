using System;
using System.Linq;
using System.Threading.Tasks;
using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.REPO.PAGINATION;
using Microsoft.EntityFrameworkCore;

namespace ARSPlatform.REPOSITORIES;

public class UserSubscriptionRepository : GenericRepository<UserSubscription>, IUserSubscriptionRepository
{
    public UserSubscriptionRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<UserSubscription?> GetByUserAndRoleAsync(int userId, string userRole)
    {
        return await _dbSet.AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId && s.UserRole == userRole);
    }

    public async Task<UserSubscription?> GetActiveByUserAndRoleAsync(int userId, string userRole)
    {
        var now = DateTime.UtcNow;
        return await _dbSet.AsNoTracking()
            .FirstOrDefaultAsync(s =>
                s.UserId == userId &&
                s.UserRole == userRole &&
                s.ExpiresAt != null &&
                s.ExpiresAt > now);
    }

    public async Task<UserSubscription?> GetLatestActiveAsync(int userId, string userRole)
    {
        var now = DateTime.UtcNow;
        return await _dbSet.AsNoTracking()
            .Where(s =>
                s.UserId == userId &&
                s.UserRole == userRole &&
                s.ExpiresAt != null &&
                s.ExpiresAt > now)
            .OrderByDescending(s => s.UpdatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> IsActiveAsync(int userId, string userRole)
    {
        var now = DateTime.UtcNow;
        return await _dbSet.AsNoTracking()
            .AnyAsync(s =>
                s.UserId == userId &&
                s.UserRole == userRole &&
                s.ExpiresAt != null &&
                s.ExpiresAt > now);
    }

    public async Task<PagedResult<UserSubscription>> GetSubscribersByAnnualFeeIdAsync(
        int annualFeeId, PaginationParams paginationParams)
    {
        var page = paginationParams.PageNumber < 1 ? 1 : paginationParams.PageNumber;
        var size = paginationParams.PageSize < 1 ? 10 : paginationParams.PageSize;

        // Lấy LatestTransactionId của tất cả transaction thuộc annualFeeId đang ACTIVE
        var validTxIds = await _context.Set<Transaction>()
            .AsNoTracking()
            .Where(t => t.AnnualFeeId == annualFeeId && t.Status == "ACTIVE")
            .Select(t => t.TransactionId)
            .ToListAsync();

        var query = _dbSet.AsNoTracking()
            .Include(s => s.User)
            .Where(s => s.LatestTransactionId != null && validTxIds.Contains(s.LatestTransactionId.Value));

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(s => s.UpdatedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();

        return new PagedResult<UserSubscription>(items, total, page, size);
    }
}
