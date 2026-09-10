using System;
using System.Linq;
using System.Threading.Tasks;
using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
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
}
