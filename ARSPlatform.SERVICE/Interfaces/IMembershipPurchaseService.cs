using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface IMembershipPurchaseService
    {
        Task<IEnumerable<MembershipPurchaseResponse>> GetAllAsync();
        Task<MembershipPurchaseResponse?> GetByIdAsync(int id);
        Task<MembershipPurchaseResponse> CreateAsync(MembershipPurchaseCreateRequest request);
        Task<MembershipPurchaseResponse?> UpdateAsync(int id, MembershipPurchaseUpdateRequest request);
        Task<bool> DeleteAsync(int id);
    }
}
