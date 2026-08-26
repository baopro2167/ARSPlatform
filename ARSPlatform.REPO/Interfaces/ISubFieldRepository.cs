using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.PAGINATION;

namespace ARSPlatform.REPO.Interfaces
{
    public interface ISubFieldRepository : IGenericRepository<SubField>
    {
        Task<IEnumerable<SubField>> GetAllWithMajorFieldAsync(int? majorFieldId = null);
        Task<PagedResult<SubField>> GetByMajorFieldIdPagedAsync(int majorFieldId, PaginationParams paginationParams);
        Task<PagedResult<SubField>> GetByMajorFieldIdPagedAsync(int majorFieldId, int pageNumber, int pageSize);
        Task<SubField?> GetByIdWithMajorFieldAsync(int id);
        Task<bool> HasUsageAsync(int id);
    }
}