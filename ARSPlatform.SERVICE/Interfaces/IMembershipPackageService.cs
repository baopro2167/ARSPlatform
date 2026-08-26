using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface IMembershipPackageService
    {
        Task<IEnumerable<MembershipPackageResponse>> GetAllAsync();
        Task<MembershipPackageResponse?> GetByIdAsync(int id);
        Task<MembershipPackageResponse> CreateAsync(MembershipPackageCreateRequest request);
        Task<MembershipPackageResponse?> UpdateAsync(int id, MembershipPackageUpdateRequest request);
        Task<bool> DeleteAsync(int id);
    }
}
