using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface IDetailedEvaluationService
    {
        Task<IEnumerable<DetailedEvaluationResponse>> GetAllAsync();
        Task<PagedResult<DetailedEvaluationResponse>> GetPagedAsync(PaginationParams paginationParams);
        Task<PagedResult<DetailedEvaluationResponse>> GetByReviewRequestIdAsync(int reviewRequestId, int pageNumber, int pageSize);
        Task<PagedResult<DetailedEvaluationResponse>> GetByReviewerIdAsync(int reviewerId, int pageNumber, int pageSize);
        Task<PagedResult<DetailedEvaluationResponse>> GetAllAsync(int pageNumber, int pageSize);
        Task<DetailedEvaluationResponse?> GetByIdAsync(int id);
        Task<DetailedEvaluationResponse> CreateAsync(DetailedEvaluationCreateRequest request, int reviewerId);
        Task<DetailedEvaluationResponse?> UpdateAsync(int id, DetailedEvaluationUpdateRequest request, int reviewerId);
        Task<bool> DeleteAsync(int id);
    }
}
