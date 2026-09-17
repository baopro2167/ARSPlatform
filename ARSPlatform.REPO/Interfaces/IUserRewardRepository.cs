using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.REPO.PAGINATION;

namespace ARSPlatform.REPO.Interfaces;

public interface IUserRewardRepository : IGenericRepository<UserReward>
{
    new Task<bool> ExistsAsync(Expression<Func<UserReward, bool>> predicate);
}
