using System.Linq;
using System.Threading.Tasks;
using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.REPO.PAGINATION;

namespace ARSPlatform.REPOSITORIES
{
    public class RoleRequestRepository : GenericRepository<RoleRequest>, IRoleRequestRepository
    {
        public RoleRequestRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<PagedResult<RoleRequest>> GetByUserIdPagedAsync(int userId, PaginationParams paginationParams)
        {
            return await GetPagedAsync(
                paginationParams,
                predicate: x => x.UserId == userId,
                orderBy: q => q.OrderByDescending(x => x.CreatedAt),
                includes: new System.Linq.Expressions.Expression<System.Func<RoleRequest, object>>[]
                {
                    x => x.User,
                    x => x.RequestedRole
                });
        }

        public async Task<PagedResult<RoleRequest>> GetByUserIdPagedAsync(int userId, int pageNumber, int pageSize)
        {
            return await GetByUserIdPagedAsync(userId, new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
        }
    }
}