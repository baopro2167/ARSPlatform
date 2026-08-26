using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface IMajorFieldService
    {
        Task<IEnumerable<MajorFieldResponse>> GetAllAsync();
        Task<MajorFieldResponse?> GetByIdAsync(int id);
        Task<MajorFieldResponse> CreateAsync(MajorFieldCreateRequest request);
        Task<MajorFieldResponse?> UpdateAsync(int id, MajorFieldUpdateRequest request);
        Task<bool> DeleteAsync(int id);
    }
}
