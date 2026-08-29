using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.PAGINATION;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface IRoleService
    {
        Task<IEnumerable<Role>> GetAllAsync();
        Task<PagedResult<Role>> GetPagedAsync(PaginationParams paginationParams);
        Task<PagedResult<Role>> GetAllAsync(int pageNumber, int pageSize);
    }
}
