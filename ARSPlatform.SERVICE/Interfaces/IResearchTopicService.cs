using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface IResearchTopicService
    {
        Task<IEnumerable<ResearchTopicResponse>> GetAllAsync(int? lecturerId = null);
        Task<PagedResult<ResearchTopicResponse>> GetPagedAsync(PaginationParams paginationParams, int? lecturerId = null);
        Task<IEnumerable<ResearchTopicResponse>> GetMyTopicsAsync(int lecturerId);
        Task<PagedResult<ResearchTopicResponse>> GetAllAsync(int pageNumber, int pageSize);
        Task<ResearchTopicResponse?> GetByIdAsync(int id);
        Task<ResearchTopicResponse> CreateAsync(ResearchTopicCreateRequest request, int? lecturerId = null);
        Task<ResearchTopicResponse?> UpdateAsync(int id, ResearchTopicUpdateRequest request);
        Task<bool> DeleteAsync(int id);
    }
}
