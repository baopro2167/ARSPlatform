using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface IResearchGroupService
    {
        Task<IEnumerable<ResearchGroupResponse>> GetAllAsync();
        Task<ResearchGroupResponse?> GetByIdAsync(int id);
        Task<ResearchGroupResponse> CreateAsync(ResearchGroupCreateRequest request);
        Task<ResearchGroupResponse?> UpdateAsync(int id, ResearchGroupUpdateRequest request);
        Task<bool> DeleteAsync(int id);
    }
}
