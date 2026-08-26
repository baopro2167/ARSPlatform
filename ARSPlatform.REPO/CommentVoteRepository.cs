using System.Linq;
using System.Threading.Tasks;
using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.REPO.PAGINATION;

namespace ARSPlatform.REPOSITORIES
{
    public class CommentVoteRepository : GenericRepository<CommentVote>, ICommentVoteRepository
    {
        public CommentVoteRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<PagedResult<CommentVote>> GetByCommentIdPagedAsync(int commentId, PaginationParams paginationParams)
        {
            return await GetPagedAsync(
                paginationParams,
                predicate: x => x.ForumCommentId == commentId,
                orderBy: q => q.OrderByDescending(x => x.CreatedAt),
                includes: new System.Linq.Expressions.Expression<System.Func<CommentVote, object>>[]
                {
                    x => x.User!
                });
        }

        public async Task<PagedResult<CommentVote>> GetByCommentIdPagedAsync(int commentId, int pageNumber, int pageSize)
        {
            return await GetByCommentIdPagedAsync(commentId, new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
        }

        public async Task<PagedResult<CommentVote>> GetByUserIdPagedAsync(int userId, PaginationParams paginationParams)
        {
            return await GetPagedAsync(
                paginationParams,
                predicate: x => x.UserId == userId,
                orderBy: q => q.OrderByDescending(x => x.CreatedAt),
                includes: new System.Linq.Expressions.Expression<System.Func<CommentVote, object>>[]
                {
                    x => x.ForumComment!
                });
        }

        public async Task<PagedResult<CommentVote>> GetByUserIdPagedAsync(int userId, int pageNumber, int pageSize)
        {
            return await GetByUserIdPagedAsync(userId, new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
        }
    }
}
