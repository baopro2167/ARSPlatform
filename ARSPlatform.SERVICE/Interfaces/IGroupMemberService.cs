using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface IGroupMemberService
    {
        Task<IEnumerable<GroupMemberResponse>> GetAllAsync();
        Task<GroupMemberResponse?> GetByIdAsync(int id);
        Task<GroupMemberResponse> CreateAsync(GroupMemberCreateRequest request);
        Task<GroupMemberResponse?> UpdateAsync(int id, GroupMemberUpdateRequest request);
        Task<bool> DeleteAsync(int id);
    }
}
