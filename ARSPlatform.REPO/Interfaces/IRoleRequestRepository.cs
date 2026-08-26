using System.Threading.Tasks;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.PAGINATION;

namespace ARSPlatform.REPO.Interfaces
{
    public interface IRoleRequestRepository : IGenericRepository<RoleRequest>
    {
        Task<PagedResult<RoleRequest>> GetByUserIdPagedAsync(int userId, PaginationParams paginationParams);
        Task<PagedResult<RoleRequest>> GetByUserIdPagedAsync(int userId, int pageNumber, int pageSize);
    }
}