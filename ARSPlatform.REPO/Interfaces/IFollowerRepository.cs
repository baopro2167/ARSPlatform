using System.Threading.Tasks;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.PAGINATION;

namespace ARSPlatform.REPO.Interfaces
{
    public interface IFollowerRepository : IGenericRepository<Follower>
    {
        Task<PagedResult<Follower>> GetByFollowedIdPagedAsync(int followedId, PaginationParams paginationParams);
        Task<PagedResult<Follower>> GetByFollowedIdPagedAsync(int followedId, int pageNumber, int pageSize);
        Task<PagedResult<Follower>> GetByFollowerIdPagedAsync(int followerId, PaginationParams paginationParams);
        Task<PagedResult<Follower>> GetByFollowerIdPagedAsync(int followerId, int pageNumber, int pageSize);
        Task<bool> IsFollowingAsync(int followerId, int followedId);
        Task<Follower?> GetRelationAsync(int followerId, int followedId);
        Task<int> GetFollowersCountAsync(int userId);
        Task<int> GetFollowingCountAsync(int userId);
        Task<IEnumerable<Follower>> GetAllWithUsersAsync();
        Task<PagedResult<Follower>> GetPagedWithUsersAsync(PaginationParams paginationParams);
    }
}
