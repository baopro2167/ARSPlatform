using System.Threading.Tasks;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.PAGINATION;

namespace ARSPlatform.REPO.Interfaces
{
    public interface ISharedMaterialRepository : IGenericRepository<SharedMaterial>
    {
        Task<PagedResult<SharedMaterial>> GetByLecturerIdPagedAsync(int lecturerId, PaginationParams paginationParams);
        Task<PagedResult<SharedMaterial>> GetByLecturerIdPagedAsync(int lecturerId, int pageNumber, int pageSize);
        Task<PagedResult<SharedMaterial>> GetByPaperIdPagedAsync(int paperId, PaginationParams paginationParams);
        Task<PagedResult<SharedMaterial>> GetByPaperIdPagedAsync(int paperId, int pageNumber, int pageSize);
    }
}
