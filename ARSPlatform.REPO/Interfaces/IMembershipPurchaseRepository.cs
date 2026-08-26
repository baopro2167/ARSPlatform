using System.Threading.Tasks;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.PAGINATION;

namespace ARSPlatform.REPO.Interfaces
{
    public interface IMembershipPurchaseRepository : IGenericRepository<MembershipPurchase>
    {
        Task<PagedResult<MembershipPurchase>> GetByUserIdPagedAsync(int userId, PaginationParams paginationParams);
        Task<PagedResult<MembershipPurchase>> GetByUserIdPagedAsync(int userId, int pageNumber, int pageSize);
        Task<PagedResult<MembershipPurchase>> GetByPackageIdPagedAsync(int packageId, PaginationParams paginationParams);
        Task<PagedResult<MembershipPurchase>> GetByPackageIdPagedAsync(int packageId, int pageNumber, int pageSize);
    }
}
