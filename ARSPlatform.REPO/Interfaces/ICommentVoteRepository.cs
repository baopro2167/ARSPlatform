using System.Threading.Tasks;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.PAGINATION;

namespace ARSPlatform.REPO.Interfaces
{
    public interface ICommentVoteRepository : IGenericRepository<CommentVote>
    {
        Task<PagedResult<CommentVote>> GetByCommentIdPagedAsync(int commentId, PaginationParams paginationParams);
        Task<PagedResult<CommentVote>> GetByCommentIdPagedAsync(int commentId, int pageNumber, int pageSize);
        Task<PagedResult<CommentVote>> GetByUserIdPagedAsync(int userId, PaginationParams paginationParams);
        Task<PagedResult<CommentVote>> GetByUserIdPagedAsync(int userId, int pageNumber, int pageSize);
    }
}
