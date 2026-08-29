using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface IReviewRequestService
    {
        Task<IEnumerable<ReviewRequestResponse>> GetAllAsync();
        Task<PagedResult<ReviewRequestResponse>> GetPagedAsync(PaginationParams paginationParams);
        Task<PagedResult<ReviewRequestResponse>> GetByReviewerIdAsync(int reviewerId, int pageNumber, int pageSize);
        Task<PagedResult<ReviewRequestResponse>> GetByPaperIdAsync(int paperId, int pageNumber, int pageSize);
        Task<PagedResult<ReviewRequestResponse>> GetAllAsync(int pageNumber, int pageSize);
        Task<ReviewRequestResponse?> GetByIdAsync(int id);
        Task<ReviewRequestResponse> CreateAsync(ReviewRequestCreateRequest request);
        Task<ReviewRequestResponse?> UpdateAsync(int id, ReviewRequestUpdateRequest request);
        Task<bool> DeleteAsync(int id);
        Task<AutoAssignReviewersResponse> AutoAssignReviewersAsync(AutoAssignReviewersRequest request);
    }
}
