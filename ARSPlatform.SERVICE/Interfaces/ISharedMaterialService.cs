using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface ISharedMaterialService
    {
        Task<IEnumerable<SharedMaterialResponse>> GetAllAsync();
        Task<SharedMaterialResponse?> GetByIdAsync(int id);
        Task<SharedMaterialResponse> CreateAsync(SharedMaterialCreateRequest request);
        Task<SharedMaterialResponse?> UpdateAsync(int id, SharedMaterialUpdateRequest request);
        Task<bool> DeleteAsync(int id);
    }
}
