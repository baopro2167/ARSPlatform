using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface IMembershipPackageService
    {
        Task<IEnumerable<MembershipPackageResponse>> GetAllAsync();
        Task<PagedResult<MembershipPackageResponse>> GetPagedAsync(PaginationParams paginationParams);
        Task<PagedResult<MembershipPackageResponse>> GetAllAsync(int pageNumber, int pageSize);
        Task<MembershipPackageResponse?> GetByIdAsync(int id);
        Task<MembershipPackageResponse> CreateAsync(MembershipPackageCreateRequest request);
        Task<MembershipPackageResponse?> UpdateAsync(int id, MembershipPackageUpdateRequest request);
        Task<bool> DeleteAsync(int id);
    }
}
