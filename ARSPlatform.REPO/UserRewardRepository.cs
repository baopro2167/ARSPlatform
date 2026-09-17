using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ARSPlatform.REPOSITORIES;

public class UserRewardRepository : GenericRepository<UserReward>, IUserRewardRepository
{
    public UserRewardRepository(AppDbContext context) : base(context)
    {
    }

    public new async Task<bool> ExistsAsync(Expression<Func<UserReward, bool>> predicate)
    {
        return await _dbSet.AnyAsync(predicate);
    }
}
