using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface IRoleRequestService
    {
        Task<IEnumerable<RoleRequestResponse>> GetAllAsync();
        Task<PagedResult<RoleRequestResponse>> GetPagedAsync(PaginationParams paginationParams);
        Task<PagedResult<RoleRequestResponse>> GetByUserIdAsync(int userId, int pageNumber, int pageSize);
        Task<PagedResult<RoleRequestResponse>> GetAllAsync(int pageNumber, int pageSize);
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
