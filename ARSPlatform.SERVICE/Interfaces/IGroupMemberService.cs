using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface IGroupMemberService
    {
        Task<IEnumerable<GroupMemberResponse>> GetAllAsync(int? groupId = null);
        Task<PagedResult<GroupMemberResponse>> GetPagedAsync(PaginationParams paginationParams, int? groupId = null);
        Task<PagedResult<GroupMemberResponse>> GetByGroupIdAsync(int groupId, int pageNumber, int pageSize);
        Task<PagedResult<GroupMemberResponse>> GetByStudentIdAsync(int studentId, int pageNumber, int pageSize);
        Task<PagedResult<GroupMemberResponse>> GetAllAsync(int pageNumber, int pageSize);
        Task<GroupMemberResponse?> GetByIdAsync(int id);
        Task<GroupMemberResponse> CreateAsync(GroupMemberCreateRequest request);
        Task<GroupMemberResponse?> UpdateAsync(int id, GroupMemberUpdateRequest request);
        Task<bool> DeleteAsync(int id);
        Task<GroupMemberResponse> SetLeaderAsync(int groupMemberId, int? userId = null);
        Task<GroupMemberResponse> RemoveLeaderAsync(int groupMemberId);
    }
}
