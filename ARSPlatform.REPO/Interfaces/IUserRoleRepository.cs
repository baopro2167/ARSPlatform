using System.Threading.Tasks;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.PAGINATION;

namespace ARSPlatform.REPO.Interfaces
{
    public interface IUserRoleRepository : IGenericRepository<UserRole>
    {
        Task<PagedResult<UserRole>> GetByUserIdPagedAsync(int userId, PaginationParams paginationParams);
        Task<PagedResult<UserRole>> GetByUserIdPagedAsync(int userId, int pageNumber, int pageSize);
    }
}
