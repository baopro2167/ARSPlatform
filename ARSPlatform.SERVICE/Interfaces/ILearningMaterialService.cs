using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface ILearningMaterialService
    {
        Task<IEnumerable<LearningMaterialResponse>> GetAllAsync();
        Task<LearningMaterialResponse?> GetByIdAsync(int id);
        Task<LearningMaterialResponse> CreateAsync(LearningMaterialCreateRequest request);
        Task<LearningMaterialResponse?> UpdateAsync(int id, LearningMaterialUpdateRequest request);
        Task<bool> DeleteAsync(int id);
    }
}
