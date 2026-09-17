using System.Threading.Tasks;
using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Interfaces;

public interface IUserRewardService
{
    // Admin CRUD
    Task<PagedResult<UserRewardResponse>> GetAllPagedAsync(UserRewardFilterParams filter);
    Task<UserRewardResponse?> GetByIdAsync(int id);
    Task<UserRewardResponse> CreateAsync(UserRewardCreateRequest request);
    Task<UserRewardResponse?> UpdateAsync(int id, UserRewardUpdateRequest request);
    Task<bool> DeleteAsync(int id);

    // Toggle status
    Task<UserRewardResponse?> ToggleAsync(int id, bool isActive);
}
