using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface ICommentVoteService
    {
        Task<IEnumerable<CommentVoteResponse>> GetAllAsync();
        Task<PagedResult<CommentVoteResponse>> GetPagedAsync(PaginationParams paginationParams);
        Task<PagedResult<CommentVoteResponse>> GetByCommentIdAsync(int commentId, int pageNumber, int pageSize);
        Task<PagedResult<CommentVoteResponse>> GetByUserIdAsync(int userId, int pageNumber, int pageSize);
        Task<PagedResult<CommentVoteResponse>> GetAllAsync(int pageNumber, int pageSize);
        Task<CommentVoteResponse> CreateAsync(CommentVoteCreateRequest request);
    }
}
