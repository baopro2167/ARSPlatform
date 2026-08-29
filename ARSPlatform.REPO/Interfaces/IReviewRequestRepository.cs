using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.PAGINATION;

namespace ARSPlatform.REPO.Interfaces
{
    public interface IReviewRequestRepository : IGenericRepository<ReviewRequest>
    {
        Task<IEnumerable<ReviewRequest>> GetAllWithReviewerAsync();
        Task<PagedResult<ReviewRequest>> GetByReviewerIdPagedAsync(int reviewerId, PaginationParams paginationParams);
        Task<PagedResult<ReviewRequest>> GetByReviewerIdPagedAsync(int reviewerId, int pageNumber, int pageSize);
        Task<PagedResult<ReviewRequest>> GetByPaperIdPagedAsync(int paperId, PaginationParams paginationParams);
        Task<PagedResult<ReviewRequest>> GetByPaperIdPagedAsync(int paperId, int pageNumber, int pageSize);
        Task<ReviewRequest?> GetByIdWithReviewerAsync(int id);
    }
}