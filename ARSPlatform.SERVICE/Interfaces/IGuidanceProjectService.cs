using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface IGuidanceProjectService
    {
        Task<IEnumerable<GuidanceProjectResponse>> GetAllAsync();
        Task<GuidanceProjectResponse?> GetByIdAsync(int id);
        Task<GuidanceProjectResponse> CreateAsync(GuidanceProjectCreateRequest request);
        Task<GuidanceProjectResponse?> UpdateAsync(int id, GuidanceProjectUpdateRequest request);
        Task<bool> DeleteAsync(int id);
    }
}
