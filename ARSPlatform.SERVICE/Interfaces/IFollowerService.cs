using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface IFollowerService
    {
        Task<IEnumerable<FollowerResponse>> GetAllAsync();
        Task<PagedResult<FollowerResponse>> GetPagedAsync(PaginationParams paginationParams);
        Task<PagedResult<FollowerResponse>> GetByFollowedIdAsync(int followedId, int pageNumber, int pageSize);
        Task<PagedResult<FollowerResponse>> GetByFollowerIdAsync(int followerId, int pageNumber, int pageSize);
        Task<PagedResult<FollowerResponse>> GetAllAsync(int pageNumber, int pageSize);
        Task<FollowerResponse> CreateAsync(FollowerCreateRequest request, int currentUserId);
        Task<FollowerResponse> FollowAsync(int currentUserId, int followedId);
        Task<bool> UnfollowAsync(int currentUserId, int followedId);
        Task<bool> ToggleFollowAsync(int currentUserId, int followedId);
        Task<bool> IsFollowingAsync(int currentUserId, int followedId);
        Task<FollowCountsResponse> GetCountsAsync(int userId);
    }
}
