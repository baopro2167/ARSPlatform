using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface IMembershipPurchaseService
    {
        Task<IEnumerable<MembershipPurchaseResponse>> GetAllAsync();
        Task<PagedResult<MembershipPurchaseResponse>> GetPagedAsync(PaginationParams paginationParams);
        Task<PagedResult<MembershipPurchaseResponse>> GetByUserIdAsync(int userId, int pageNumber, int pageSize);
        Task<PagedResult<MembershipPurchaseResponse>> GetByPackageIdAsync(int packageId, int pageNumber, int pageSize);
        Task<PagedResult<MembershipPurchaseResponse>> GetAllAsync(int pageNumber, int pageSize);
        Task<MembershipPurchaseResponse?> GetByIdAsync(int id);
        Task<MembershipPurchaseResponse> CreateAsync(MembershipPurchaseCreateRequest request);
        Task<MembershipPurchaseResponse?> UpdateAsync(int id, MembershipPurchaseUpdateRequest request);
        Task<bool> DeleteAsync(int id);
    }
}
