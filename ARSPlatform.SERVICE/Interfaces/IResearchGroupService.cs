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
        Task<IEnumerable<ResearchGroupResponse>> GetMyGroupsAsync(int currentUserId);
        Task<PagedResult<ResearchGroupResponse>> GetPagedAsync(PaginationParams paginationParams);
        Task<PagedResult<ResearchGroupResponse>> GetByLecturerIdAsync(int lecturerId, int pageNumber, int pageSize);
        Task<PagedResult<ResearchGroupResponse>> GetByTopicIdAsync(int topicId, int pageNumber, int pageSize);
        Task<PagedResult<ResearchGroupResponse>> GetAllAsync(int pageNumber, int pageSize);
        Task<ResearchGroupResponse?> GetByIdAsync(int id);
        Task<ResearchGroupResponse> CreateAsync(ResearchGroupCreateRequest request, int? lecturerId = null);
        Task<ResearchGroupResponse?> UpdateAsync(int id, ResearchGroupUpdateRequest request);
        Task<bool> DeleteAsync(int id);
        Task<ResearchGroupInviteResponse> InviteStudentsAsync(int researchGroupId, ResearchGroupInviteRequest request, int currentUserId);
    }
}
