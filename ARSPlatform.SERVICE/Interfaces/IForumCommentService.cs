using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface IForumCommentService
    {
        Task<IEnumerable<ForumCommentResponse>> GetAllAsync(int? postId = null);
        Task<PagedResult<ForumCommentResponse>> GetPagedAsync(PaginationParams paginationParams, int? postId = null);
        Task<PagedResult<ForumCommentResponse>> GetByPostIdAsync(int postId, int pageNumber, int pageSize);
        Task<PagedResult<ForumCommentResponse>> GetByUserIdAsync(int userId, int pageNumber, int pageSize);
        Task<PagedResult<ForumCommentResponse>> GetAllAsync(int pageNumber, int pageSize);
        Task<ForumCommentResponse?> GetByIdAsync(int id);
        Task<ForumCommentResponse> CreateAsync(ForumCommentCreateRequest request, int userId);
        Task<ForumCommentResponse?> UpdateAsync(int id, ForumCommentUpdateRequest request);
        Task<bool> DeleteAsync(int id);
    }
}
