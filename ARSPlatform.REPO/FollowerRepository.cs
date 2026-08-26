using System.Linq;
using System.Threading.Tasks;
using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.REPO.PAGINATION;

namespace ARSPlatform.REPOSITORIES
{
    public class FollowerRepository : GenericRepository<Follower>, IFollowerRepository
    {
        public FollowerRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<PagedResult<Follower>> GetByFollowedIdPagedAsync(int followedId, PaginationParams paginationParams)
        {
            return await GetPagedAsync(
                paginationParams,
                predicate: x => x.FollowedId == followedId,
                orderBy: q => q.OrderByDescending(x => x.CreatedAt),
                includes: new System.Linq.Expressions.Expression<System.Func<Follower, object>>[]
                {
                    x => x.FollowerNavigation,
                    x => x.Followed
                });
        }

        public async Task<PagedResult<Follower>> GetByFollowedIdPagedAsync(int followedId, int pageNumber, int pageSize)
        {
            return await GetByFollowedIdPagedAsync(followedId, new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
        }

        public async Task<PagedResult<Follower>> GetByFollowerIdPagedAsync(int followerId, PaginationParams paginationParams)
        {
            return await GetPagedAsync(
                paginationParams,
                predicate: x => x.FollowerId == followerId,
                orderBy: q => q.OrderByDescending(x => x.CreatedAt),
                includes: new System.Linq.Expressions.Expression<System.Func<Follower, object>>[]
                {
                    x => x.FollowerNavigation,
                    x => x.Followed
                });
        }

        public async Task<PagedResult<Follower>> GetByFollowerIdPagedAsync(int followerId, int pageNumber, int pageSize)
        {
            return await GetByFollowerIdPagedAsync(followerId, new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
        }
    }
}
