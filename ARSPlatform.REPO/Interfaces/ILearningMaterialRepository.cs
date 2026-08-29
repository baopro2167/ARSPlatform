using System.Threading.Tasks;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.PAGINATION;

namespace ARSPlatform.REPO.Interfaces
{
    public interface ILearningMaterialRepository : IGenericRepository<LearningMaterial>
    {
        Task<PagedResult<LearningMaterial>> GetByLecturerIdPagedAsync(int lecturerId, PaginationParams paginationParams);
        Task<PagedResult<LearningMaterial>> GetByLecturerIdPagedAsync(int lecturerId, int pageNumber, int pageSize);
        Task<PagedResult<LearningMaterial>> GetBySubFieldIdPagedAsync(int subFieldId, PaginationParams paginationParams);
        Task<PagedResult<LearningMaterial>> GetBySubFieldIdPagedAsync(int subFieldId, int pageNumber, int pageSize);
    }
}
