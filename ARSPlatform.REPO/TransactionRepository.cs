using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.REPO.PAGINATION;
using Microsoft.EntityFrameworkCore;

namespace ARSPlatform.REPOSITORIES
{
    public class TransactionRepository : GenericRepository<Transaction>, ITransactionRepository
    {
        public TransactionRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<Transaction?> GetByOrderCodeAsync(string orderCode)
        {
            return await _context.Transactions
                .FirstOrDefaultAsync(t => t.PaymentOrderId == orderCode);
        }

        public async Task<PagedResult<Transaction>> GetByWalletIdPagedAsync(int walletId, PaginationParams paginationParams)
        {
            return await GetPagedAsync(
                paginationParams,
                predicate: x => x.WalletId == walletId,
                orderBy: q => q.OrderByDescending(x => x.CreatedAt),
                includes: new System.Linq.Expressions.Expression<System.Func<Transaction, object>>[]
                {
                    x => x.Wallet!
                });
        }

        public async Task<PagedResult<Transaction>> GetByWalletIdPagedAsync(int walletId, int pageNumber, int pageSize)
        {
            return await GetByWalletIdPagedAsync(walletId, new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
        }
    }
}
