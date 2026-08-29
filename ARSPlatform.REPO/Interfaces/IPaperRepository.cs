using System;
using System.Threading.Tasks;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.PAGINATION;

namespace ARSPlatform.REPO.Interfaces
{
    public interface IPaperRepository : IGenericRepository<Paper>
    {
        Task<Paper?> GetWithAuthorByIdAsync(int id);
        Task<PagedResult<Paper>> GetByAuthorIdPagedAsync(int authorId, PaginationParams paginationParams);
        Task<PagedResult<Paper>> GetByAuthorIdPagedAsync(int authorId, int pageNumber, int pageSize);
        Task<PagedResult<Paper>> GetBySubFieldIdPagedAsync(int subFieldId, PaginationParams paginationParams);
        Task<PagedResult<Paper>> GetBySubFieldIdPagedAsync(int subFieldId, int pageNumber, int pageSize);
    }
}
