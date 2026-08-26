using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface IDetailedEvaluationService
    {
        Task<IEnumerable<DetailedEvaluationResponse>> GetAllAsync();
        Task<DetailedEvaluationResponse?> GetByIdAsync(int id);
        Task<DetailedEvaluationResponse> CreateAsync(DetailedEvaluationCreateRequest request, int reviewerId);
        Task<DetailedEvaluationResponse?> UpdateAsync(int id, DetailedEvaluationUpdateRequest request, int reviewerId);
        Task<bool> DeleteAsync(int id);
    }
}
