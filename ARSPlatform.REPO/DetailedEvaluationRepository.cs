using System.Linq;
using System.Threading.Tasks;
using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.REPO.PAGINATION;

namespace ARSPlatform.REPOSITORIES
{
    public class DetailedEvaluationRepository : GenericRepository<DetailedEvaluation>, IDetailedEvaluationRepository
    {
        public DetailedEvaluationRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<PagedResult<DetailedEvaluation>> GetByReviewRequestIdPagedAsync(int reviewRequestId, PaginationParams paginationParams)
        {
            return await GetPagedAsync(
                paginationParams,
                predicate: x => x.ReviewRequestId == reviewRequestId,
                orderBy: q => q.OrderByDescending(x => x.CreatedAt),
                includes: new System.Linq.Expressions.Expression<System.Func<DetailedEvaluation, object>>[]
                {
                    x => x.Reviewer!,
                    x => x.ReviewRequest!
                });
        }

        public async Task<PagedResult<DetailedEvaluation>> GetByReviewRequestIdPagedAsync(int reviewRequestId, int pageNumber, int pageSize)
        {
            return await GetByReviewRequestIdPagedAsync(reviewRequestId, new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
        }

        public async Task<PagedResult<DetailedEvaluation>> GetByReviewerIdPagedAsync(int reviewerId, PaginationParams paginationParams)
        {
            return await GetPagedAsync(
                paginationParams,
                predicate: x => x.ReviewerId == reviewerId,
                orderBy: q => q.OrderByDescending(x => x.CreatedAt),
                includes: new System.Linq.Expressions.Expression<System.Func<DetailedEvaluation, object>>[]
                {
                    x => x.Reviewer!,
                    x => x.ReviewRequest!
                });
        }

        public async Task<PagedResult<DetailedEvaluation>> GetByReviewerIdPagedAsync(int reviewerId, int pageNumber, int pageSize)
        {
            return await GetByReviewerIdPagedAsync(reviewerId, new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
        }
    }
}
