using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface IWalletService
    {
        Task<IEnumerable<WalletResponse>> GetAllAsync(int? userId = null);
        Task<WalletResponse?> GetByIdAsync(int id);
        Task<WalletResponse> CreateAsync(WalletCreateRequest request);
        Task<WalletResponse?> UpdateAsync(int id, WalletUpdateRequest request);
        Task<bool> DeleteAsync(int id);
    }
}
