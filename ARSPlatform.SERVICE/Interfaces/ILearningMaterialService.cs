using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface ILearningMaterialService
    {
        Task<IEnumerable<LearningMaterialResponse>> GetAllAsync();
        Task<PagedResult<LearningMaterialResponse>> GetPagedAsync(PaginationParams paginationParams);
        Task<PagedResult<LearningMaterialResponse>> GetByLecturerIdAsync(int lecturerId, int pageNumber, int pageSize);
        Task<PagedResult<LearningMaterialResponse>> GetBySubFieldIdAsync(int subFieldId, int pageNumber, int pageSize);
        Task<PagedResult<LearningMaterialResponse>> GetAllAsync(int pageNumber, int pageSize);
        Task<LearningMaterialResponse?> GetByIdAsync(int id);
        Task<LearningMaterialResponse> CreateAsync(LearningMaterialCreateRequest request);
        Task<LearningMaterialResponse?> UpdateAsync(int id, LearningMaterialUpdateRequest request);
        Task<bool> DeleteAsync(int id);
    }
}
