using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface IWithdrawalRequestService
    {
        Task<IEnumerable<WithdrawalRequestResponse>> GetAllAsync(int? walletId = null);
        Task<PagedResult<WithdrawalRequestResponse>> GetPagedAsync(PaginationParams paginationParams, int? walletId = null);
        Task<PagedResult<WithdrawalRequestResponse>> GetByWalletIdAsync(int walletId, int pageNumber, int pageSize);
        Task<PagedResult<WithdrawalRequestResponse>> GetAllAsync(int pageNumber, int pageSize);
        Task<WithdrawalRequestResponse?> GetByIdAsync(int id);
        Task<WithdrawalRequestResponse> CreateAsync(WithdrawalRequestCreateRequest request);
        Task<bool> DeleteAsync(int id);
    }
}
