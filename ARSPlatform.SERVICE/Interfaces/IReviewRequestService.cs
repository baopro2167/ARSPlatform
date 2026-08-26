using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface IReviewRequestService
    {
        Task<IEnumerable<ReviewRequestResponse>> GetAllAsync();
        Task<ReviewRequestResponse?> GetByIdAsync(int id);
        Task<ReviewRequestResponse> CreateAsync(ReviewRequestCreateRequest request);
        Task<ReviewRequestResponse?> UpdateAsync(int id, ReviewRequestUpdateRequest request);
        Task<bool> DeleteAsync(int id);
    }
}
