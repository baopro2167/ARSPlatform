using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
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

        public async Task<(bool isUpvoted, int upvoteCount)> ToggleVoteAsync(int commentId, int userId)
        {
            var comment = await _context.ForumComments.FirstOrDefaultAsync(c => c.ForumCommentId == commentId);
            if (comment == null)
            {
                throw new System.Collections.Generic.KeyNotFoundException($"Forum comment with ID {commentId} does not exist.");
            }

            var existingVote = await _context.CommentVotes
                .FirstOrDefaultAsync(v => v.ForumCommentId == commentId && v.UserId == userId);

            bool isUpvoted;
            if (existingVote != null)
            {
                _context.CommentVotes.Remove(existingVote);
                comment.UpvoteCount = System.Math.Max(0, (comment.UpvoteCount ?? 0) - 1);
                isUpvoted = false;
            }
            else
            {
                var newVote = new CommentVote
                {
                    ForumCommentId = commentId,
                    UserId = userId,
                    CreatedAt = System.DateTime.UtcNow
                };
                await _context.CommentVotes.AddAsync(newVote);
                comment.UpvoteCount = (comment.UpvoteCount ?? 0) + 1;
                isUpvoted = true;
            }

            await _context.SaveChangesAsync();
            return (isUpvoted, comment.UpvoteCount ?? 0);
        }

        public async Task<bool> IsCommentVotedAsync(int commentId, int userId)
        {
            return await _context.CommentVotes.AnyAsync(v => v.ForumCommentId == commentId && v.UserId == userId);
        }

        public async Task<System.Collections.Generic.List<int>> GetVotedCommentIdsByUserAsync(int userId, System.Collections.Generic.IEnumerable<int> commentIds)
        {
            var idsList = commentIds.ToList();
            if (!idsList.Any()) return new System.Collections.Generic.List<int>();

            return await _context.CommentVotes
                .Where(v => v.UserId == userId && idsList.Contains(v.ForumCommentId))
                .Select(v => v.ForumCommentId)
                .ToListAsync();
        }

        public async Task<System.Collections.Generic.List<int>> GetAllVotedCommentIdsByUserAsync(int userId)
        {
            return await _context.CommentVotes
                .Where(v => v.UserId == userId)
                .Select(v => v.ForumCommentId)
                .ToListAsync();
        }
    }
}
