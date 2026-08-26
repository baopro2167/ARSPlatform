using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface ITransactionService
    {
        Task<IEnumerable<TransactionResponse>> GetAllAsync(int? walletId = null);
        Task<PagedResult<TransactionResponse>> GetPagedAsync(PaginationParams paginationParams, int? walletId = null);
        Task<PagedResult<TransactionResponse>> GetByWalletIdAsync(int walletId, int pageNumber, int pageSize);
        Task<PagedResult<TransactionResponse>> GetAllAsync(int pageNumber, int pageSize);
        Task<TransactionResponse?> GetByIdAsync(int id);
        Task<TransactionResponse> CreateAsync(TransactionCreateRequest request);
        Task<TransactionResponse?> UpdateAsync(int id, TransactionUpdateRequest request);
        Task<bool> DeleteAsync(int id);
    }
}
