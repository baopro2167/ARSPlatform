using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.REPO.PAGINATION;
using Microsoft.EntityFrameworkCore;

namespace ARSPlatform.REPOSITORIES
{
    public class ReviewRequestRepository : GenericRepository<ReviewRequest>, IReviewRequestRepository
    {
        public ReviewRequestRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<ReviewRequest>> GetAllWithReviewerAsync()
        {
            return await _dbSet
                .AsNoTracking()
                .Include(x => x.Reviewer)
                .Include(x => x.Paper)
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.ReviewRequestId)
                .ToListAsync();
        }

        public async Task<ReviewRequest?> GetByIdWithReviewerAsync(int id)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(x => x.Reviewer)
                .Include(x => x.Paper)
                .FirstOrDefaultAsync(x => x.ReviewRequestId == id);
        }

        public async Task<PagedResult<ReviewRequest>> GetByReviewerIdPagedAsync(int reviewerId, PaginationParams paginationParams)
        {
            return await GetPagedAsync(
                paginationParams,
                predicate: x => x.ReviewerId == reviewerId,
                orderBy: q => q.OrderByDescending(x => x.CreatedAt),
                includes: new System.Linq.Expressions.Expression<System.Func<ReviewRequest, object>>[]
                {
                    x => x.Reviewer!,
                    x => x.Paper!
                });
        }

        public async Task<PagedResult<ReviewRequest>> GetByReviewerIdPagedAsync(int reviewerId, int pageNumber, int pageSize)
        {
            return await GetByReviewerIdPagedAsync(reviewerId, new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
        }

        public async Task<PagedResult<ReviewRequest>> GetByPaperIdPagedAsync(int paperId, PaginationParams paginationParams)
        {
            return await GetPagedAsync(
                paginationParams,
                predicate: x => x.PaperId == paperId,
                orderBy: q => q.OrderByDescending(x => x.CreatedAt),
                includes: new System.Linq.Expressions.Expression<System.Func<ReviewRequest, object>>[]
                {
                    x => x.Reviewer!,
                    x => x.Paper!
                });
        }

        public async Task<PagedResult<ReviewRequest>> GetByPaperIdPagedAsync(int paperId, int pageNumber, int pageSize)
        {
            return await GetByPaperIdPagedAsync(paperId, new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
        }
    }
}