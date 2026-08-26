using System.Threading.Tasks;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.PAGINATION;

namespace ARSPlatform.REPO.Interfaces
{
    public interface IWithdrawalRequestRepository : IGenericRepository<WithdrawalRequest>
    {
        Task<PagedResult<WithdrawalRequest>> GetByWalletIdPagedAsync(int walletId, PaginationParams paginationParams);
        Task<PagedResult<WithdrawalRequest>> GetByWalletIdPagedAsync(int walletId, int pageNumber, int pageSize);
    }
}