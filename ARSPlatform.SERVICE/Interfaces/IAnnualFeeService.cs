using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface IAnnualFeeService
    {
        Task<IEnumerable<AnnualFeeResponse>> GetAllAsync(bool? isActive = null, string? targetRole = null, string? billingCycle = null);
        Task<AnnualFeeResponse?> GetByIdAsync(int id);
        Task<AnnualFeeResponse> CreateAsync(AnnualFeeCreateRequest request);
        Task<AnnualFeeResponse?> UpdateAsync(int id, AnnualFeeUpdateRequest request);
        Task<AnnualFeeResponse?> ToggleActiveAsync(int id);
        Task<bool> DeleteAsync(int id);
    }
}
