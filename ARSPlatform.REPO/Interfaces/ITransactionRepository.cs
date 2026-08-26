using System.Threading.Tasks;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.PAGINATION;

namespace ARSPlatform.REPO.Interfaces
{
    public interface ITransactionRepository : IGenericRepository<Transaction>
    {
        Task<Transaction?> GetByOrderCodeAsync(string orderCode);
        Task<PagedResult<Transaction>> GetByWalletIdPagedAsync(int walletId, PaginationParams paginationParams);
        Task<PagedResult<Transaction>> GetByWalletIdPagedAsync(int walletId, int pageNumber, int pageSize);
    }
}
