using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface IForumPostService
    {
        Task<IEnumerable<ForumPostResponse>> GetAllAsync(string? category = null, string? sort = null, string? search = null);
        Task<PagedResult<ForumPostResponse>> GetPagedAsync(PaginationParams paginationParams, string? category = null, string? sort = null, string? search = null);
        Task<PagedResult<ForumPostResponse>> GetAllAsync(int pageNumber, int pageSize);
        Task<ForumPostResponse?> GetByIdAsync(int id);
        Task<ForumPostResponse> CreateAsync(ForumPostCreateRequest request, int userId);
    }
}
