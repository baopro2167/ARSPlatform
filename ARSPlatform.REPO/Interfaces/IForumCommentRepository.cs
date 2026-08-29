using System.Threading.Tasks;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.PAGINATION;

namespace ARSPlatform.REPO.Interfaces
{
    public interface IForumCommentRepository : IGenericRepository<ForumComment>
    {
        Task<PagedResult<ForumComment>> GetByPostIdPagedAsync(int postId, PaginationParams paginationParams);
        Task<PagedResult<ForumComment>> GetByPostIdPagedAsync(int postId, int pageNumber, int pageSize);
        Task<PagedResult<ForumComment>> GetByUserIdPagedAsync(int userId, PaginationParams paginationParams);
        Task<PagedResult<ForumComment>> GetByUserIdPagedAsync(int userId, int pageNumber, int pageSize);
    }
}
