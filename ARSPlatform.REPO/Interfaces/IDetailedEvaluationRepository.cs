using System.Threading.Tasks;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.PAGINATION;

namespace ARSPlatform.REPO.Interfaces
{
    public interface IDetailedEvaluationRepository : IGenericRepository<DetailedEvaluation>
    {
        Task<PagedResult<DetailedEvaluation>> GetByReviewRequestIdPagedAsync(int reviewRequestId, PaginationParams paginationParams);
        Task<PagedResult<DetailedEvaluation>> GetByReviewRequestIdPagedAsync(int reviewRequestId, int pageNumber, int pageSize);
        Task<PagedResult<DetailedEvaluation>> GetByReviewerIdPagedAsync(int reviewerId, PaginationParams paginationParams);
        Task<PagedResult<DetailedEvaluation>> GetByReviewerIdPagedAsync(int reviewerId, int pageNumber, int pageSize);
    }
}
