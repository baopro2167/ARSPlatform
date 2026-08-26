using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface IResearchGroupService
    {
        Task<IEnumerable<ResearchGroupResponse>> GetAllAsync();
        Task<PagedResult<ResearchGroupResponse>> GetPagedAsync(PaginationParams paginationParams);
        Task<PagedResult<ResearchGroupResponse>> GetByLecturerIdAsync(int lecturerId, int pageNumber, int pageSize);
        Task<PagedResult<ResearchGroupResponse>> GetByTopicIdAsync(int topicId, int pageNumber, int pageSize);
        Task<PagedResult<ResearchGroupResponse>> GetAllAsync(int pageNumber, int pageSize);
        Task<ResearchGroupResponse?> GetByIdAsync(int id);
        Task<ResearchGroupResponse> CreateAsync(ResearchGroupCreateRequest request);
        Task<ResearchGroupResponse?> UpdateAsync(int id, ResearchGroupUpdateRequest request);
        Task<bool> DeleteAsync(int id);
    }
}
