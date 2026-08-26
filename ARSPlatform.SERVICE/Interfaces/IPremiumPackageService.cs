using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface IPremiumPackageService
    {
        Task<IEnumerable<PremiumPackageResponse>> GetAllAsync();
        Task<PagedResult<PremiumPackageResponse>> GetPagedAsync(PaginationParams paginationParams);
        Task<PagedResult<PremiumPackageResponse>> GetAllAsync(int pageNumber, int pageSize);
        Task<PremiumPackageResponse> CreateAsync(PremiumPackageCreateRequest request);
        Task<PremiumPackageResponse?> UpdateAsync(int id, PremiumPackageUpdateRequest request);
        Task<PremiumPackageResponse?> ToggleAsync(int id, bool isActive);
        Task<bool> DeleteAsync(int id);
    }
}
