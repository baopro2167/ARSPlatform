using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.PAGINATION;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ARSPlatform.REPO.Interfaces
{
    public interface IForumPostRepository
        : IGenericRepository<ForumPost>
    {
        Task<IEnumerable<ForumPost>> SearchAsync(
            string? search,
            string? category,
            string? sort);

        Task<PagedResult<ForumPost>> SearchPagedAsync(
            PaginationParams paginationParams,
            string? search,
            string? category,
            string? sort);
    }
}