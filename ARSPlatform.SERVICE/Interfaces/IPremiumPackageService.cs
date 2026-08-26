using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface IPremiumPackageService
    {
        Task<IEnumerable<PremiumPackageResponse>> GetAllAsync();
        Task<PremiumPackageResponse> CreateAsync(PremiumPackageCreateRequest request);
        Task<PremiumPackageResponse?> UpdateAsync(int id, PremiumPackageUpdateRequest request);
        Task<PremiumPackageResponse?> ToggleAsync(int id, bool isActive);
        Task<bool> DeleteAsync(int id);
    }
}
