using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface ITransactionService
    {
        Task<IEnumerable<TransactionResponse>> GetAllAsync();
        Task<TransactionResponse?> GetByIdAsync(int id);
        Task<TransactionResponse> CreateAsync(TransactionCreateRequest request);
        Task<TransactionResponse?> UpdateAsync(int id, TransactionUpdateRequest request);
        Task<bool> DeleteAsync(int id);
    }
}
