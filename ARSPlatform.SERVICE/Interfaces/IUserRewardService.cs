using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Interfaces
{
    /// <summary>
    /// Service quản lý UserRewards (phần thưởng cho Researcher).
    /// </summary>
    public interface IUserRewardService
    {
        Task<PagedResult<UserRewardResponse>> GetPagedAsync(UserRewardPaginationRequest request);
        Task<UserRewardResponse?> GetByIdAsync(int id);
        Task<UserRewardResponse> CreateAsync(UserRewardCreateRequest request);
        Task<UserRewardResponse?> UpdateAsync(int id, UserRewardUpdateRequest request);
        Task<bool> DeleteAsync(int id);
        Task<UserRewardResponse?> UpdateStatusAsync(int id, string status);

        /// <summary>
        /// Dùng nội bộ khi paper publish: lookup reward đầu tiên
        /// có Name chứa keyword (case-insensitive) và Status = "Active".
        /// </summary>
        Task<UserRewardResponse?> FindActiveByNameContainsAsync(string keyword);
    }
}
