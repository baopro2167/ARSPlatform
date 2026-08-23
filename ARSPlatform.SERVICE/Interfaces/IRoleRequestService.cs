using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface IRoleRequestService
    {
        Task<IEnumerable<RoleRequestResponse>> GetAllAsync();
        Task<RoleRequestResponse?> GetByIdAsync(int id);
        Task<RoleRequestResponse> ApproveAsync(
            int id,
            int adminId,
            RoleRequestDecisionRequest request);
        Task<RoleRequestResponse> DenyAsync(
            int id,
            int adminId,
            RoleRequestDecisionRequest request);
    }
}
