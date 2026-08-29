using System.Linq;
using System.Threading.Tasks;
using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.REPO.PAGINATION;

namespace ARSPlatform.REPOSITORIES
{
    public class ForumCommentRepository : GenericRepository<ForumComment>, IForumCommentRepository
    {
        public ForumCommentRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<PagedResult<ForumComment>> GetByPostIdPagedAsync(int postId, PaginationParams paginationParams)
        {
            return await GetPagedAsync(
                paginationParams,
                predicate: x => x.ForumPostId == postId,
                orderBy: q => q.OrderBy(x => x.CreatedAt),
                includes: new System.Linq.Expressions.Expression<System.Func<ForumComment, object>>[]
                {
                    x => x.User!
                });
        }

        public async Task<PagedResult<ForumComment>> GetByPostIdPagedAsync(int postId, int pageNumber, int pageSize)
        {
            return await GetByPostIdPagedAsync(postId, new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
        }

        public async Task<PagedResult<ForumComment>> GetByUserIdPagedAsync(int userId, PaginationParams paginationParams)
        {
            return await GetPagedAsync(
                paginationParams,
                predicate: x => x.UserId == userId,
                orderBy: q => q.OrderByDescending(x => x.CreatedAt),
                includes: new System.Linq.Expressions.Expression<System.Func<ForumComment, object>>[]
                {
                    x => x.User!
                });
        }

        public async Task<PagedResult<ForumComment>> GetByUserIdPagedAsync(int userId, int pageNumber, int pageSize)
        {
            return await GetByUserIdPagedAsync(userId, new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
        }
    }
}
