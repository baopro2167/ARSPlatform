using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface IResearchTopicService
    {
        Task<IEnumerable<ResearchTopicResponse>> GetAllAsync();
        Task<ResearchTopicResponse?> GetByIdAsync(int id);
        Task<ResearchTopicResponse> CreateAsync(ResearchTopicCreateRequest request);
        Task<ResearchTopicResponse?> UpdateAsync(int id, ResearchTopicUpdateRequest request);
        Task<bool> DeleteAsync(int id);
    }
}
