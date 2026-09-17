using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.REPO.PAGINATION;
using Microsoft.EntityFrameworkCore;

namespace ARSPlatform.REPOSITORIES
{
    public class UserRewardRepository : GenericRepository<UserReward>, IUserRewardRepository
    {
        public UserRewardRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<PagedResult<UserReward>> GetPagedAsync(
            PaginationParams paginationParams,
            string? status = null,
            string? search = null)
        {
            IQueryable<UserReward> query = _dbSet;

            if (!string.IsNullOrWhiteSpace(status))
            {
                var s = status.Trim();
                query = query.Where(x => x.Status == s);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var kw = search.Trim().ToLower();
                query = query.Where(x => x.Name != null && x.Name.ToLower().Contains(kw));
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(x => x.UpdateAt)
                .ThenByDescending(x => x.Id)
                .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
                .Take(paginationParams.PageSize)
                .ToListAsync();

            return new PagedResult<UserReward>(items, total, paginationParams.PageNumber, paginationParams.PageSize);
        }

        public async Task<UserReward?> FindActiveByNameContainsAsync(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return null;
            var kw = keyword.Trim().ToLower();
            return await _dbSet
                .Where(x => x.Status == "Active"
                    && x.Name != null
                    && x.Name.ToLower().Contains(kw))
                .OrderBy(x => x.Id)
                .FirstOrDefaultAsync();
        }

        public async Task<int> CountActiveAsync()
        {
            return await _dbSet.CountAsync(x => x.Status == "Active");
        }
    }
}
