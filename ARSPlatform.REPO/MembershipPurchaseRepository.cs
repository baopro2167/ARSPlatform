using System.Linq;
using System.Threading.Tasks;
using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.REPO.PAGINATION;

namespace ARSPlatform.REPOSITORIES
{
    public class MembershipPurchaseRepository : GenericRepository<MembershipPurchase>, IMembershipPurchaseRepository
    {
        public MembershipPurchaseRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<PagedResult<MembershipPurchase>> GetByUserIdPagedAsync(int userId, PaginationParams paginationParams)
        {
            return await GetPagedAsync(
                paginationParams,
                predicate: x => x.UserId == userId,
                orderBy: q => q.OrderByDescending(x => x.PurchasedAt),
                includes: new System.Linq.Expressions.Expression<System.Func<MembershipPurchase, object>>[]
                {
                    x => x.Package!
                });
        }

        public async Task<PagedResult<MembershipPurchase>> GetByUserIdPagedAsync(int userId, int pageNumber, int pageSize)
        {
            return await GetByUserIdPagedAsync(userId, new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
        }

        public async Task<PagedResult<MembershipPurchase>> GetByPackageIdPagedAsync(int packageId, PaginationParams paginationParams)
        {
            return await GetPagedAsync(
                paginationParams,
                predicate: x => x.PackageId == packageId,
                orderBy: q => q.OrderByDescending(x => x.PurchasedAt),
                includes: new System.Linq.Expressions.Expression<System.Func<MembershipPurchase, object>>[]
                {
                    x => x.User!
                });
        }

        public async Task<PagedResult<MembershipPurchase>> GetByPackageIdPagedAsync(int packageId, int pageNumber, int pageSize)
        {
            return await GetByPackageIdPagedAsync(packageId, new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
        }
    }
}
