using System.Linq;
using System.Threading.Tasks;
using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.REPO.PAGINATION;
using Microsoft.EntityFrameworkCore;

namespace ARSPlatform.REPOSITORIES
{
    public class UserRoleRepository : GenericRepository<UserRole>, IUserRoleRepository
    {
        public UserRoleRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<PagedResult<UserRole>> GetByUserIdPagedAsync(int userId, PaginationParams paginationParams)
        {
            return await GetPagedAsync(
                paginationParams,
                predicate: x => x.UserId == userId,
                orderBy: q => q.OrderBy(x => x.UserRoleId),
                includes: x => x.Role!);
        }

        public async Task<PagedResult<UserRole>> GetByUserIdPagedAsync(int userId, int pageNumber, int pageSize)
        {
            return await GetByUserIdPagedAsync(userId, new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
        }
        public async Task<bool> UserHasRoleAsync(int userId, string roleName)
        {
            return await _context.UserRoles
                .AnyAsync(ur => ur.UserId == userId && ur.Role != null && ur.Role.Name == roleName);
        }
    }
}
