using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface IWalletService
    {
        Task<IEnumerable<WalletResponse>> GetAllAsync(int? userId = null);
        Task<PagedResult<WalletResponse>> GetPagedAsync(PaginationParams paginationParams, int? userId = null);
        Task<PagedResult<WalletResponse>> GetByUserIdAsync(int userId, int pageNumber, int pageSize);
        Task<PagedResult<WalletResponse>> GetAllAsync(int pageNumber, int pageSize);
        Task<WalletResponse?> GetByIdAsync(int id);
        Task<WalletResponse> CreateAsync(WalletCreateRequest request);
        Task<WalletResponse?> UpdateAsync(int id, WalletUpdateRequest request);
        Task<bool> DeleteAsync(int id);
    }
}
