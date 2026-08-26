using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface ISubFieldService
    {
        Task<IEnumerable<SubFieldResponse>> GetAllAsync(int? majorFieldId = null);
        Task<SubFieldResponse?> GetByIdAsync(int id);
        Task<SubFieldResponse> CreateAsync(SubFieldCreateRequest request);
        Task<SubFieldResponse?> UpdateAsync(int id, SubFieldUpdateRequest request);
        Task<bool> DeleteAsync(int id);
    }
}
