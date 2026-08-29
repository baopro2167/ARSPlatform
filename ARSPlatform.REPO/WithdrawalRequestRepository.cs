using System.Linq;
using System.Threading.Tasks;
using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.REPO.PAGINATION;

namespace ARSPlatform.REPOSITORIES
{
    public class WithdrawalRequestRepository
        : GenericRepository<WithdrawalRequest>,
          IWithdrawalRequestRepository
    {
        public WithdrawalRequestRepository(AppDbContext context)
            : base(context)
        {
        }

        public async Task<PagedResult<WithdrawalRequest>> GetByWalletIdPagedAsync(int walletId, PaginationParams paginationParams)
        {
            return await GetPagedAsync(
                paginationParams,
                predicate: x => x.WalletId == walletId,
                orderBy: (System.Linq.IQueryable<WithdrawalRequest> q) => q.OrderByDescending(x => x.CreatedAt),
                includes: new System.Linq.Expressions.Expression<System.Func<WithdrawalRequest, object>>[]
                {
                    x => x.Wallet!
                });
        }

        public async Task<PagedResult<WithdrawalRequest>> GetByWalletIdPagedAsync(int walletId, int pageNumber, int pageSize)
        {
            return await GetByWalletIdPagedAsync(walletId, new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
        }
    }
}