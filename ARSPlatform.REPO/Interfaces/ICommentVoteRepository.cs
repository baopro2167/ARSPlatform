using System.Threading.Tasks;
using System.Collections.Generic;
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
        Task<(bool isUpvoted, int upvoteCount)> ToggleVoteAsync(int commentId, int userId);
        Task<bool> IsCommentVotedAsync(int commentId, int userId);
        Task<List<int>> GetVotedCommentIdsByUserAsync(int userId, IEnumerable<int> commentIds);
        Task<List<int>> GetAllVotedCommentIdsByUserAsync(int userId);
        Task<int> CountGivenByUserIdAsync(int userId);
        Task<int> GetMaxVotesOnCommentByUserAsync(int userId);
    }
}
