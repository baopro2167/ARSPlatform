using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface IWithdrawalRequestService
    {
        Task<IEnumerable<WithdrawalRequestResponse>> GetAllAsync();
        Task<WithdrawalRequestResponse?> GetByIdAsync(int id);
        Task<WithdrawalRequestResponse> CreateAsync(WithdrawalRequestCreateRequest request);
    }
}
