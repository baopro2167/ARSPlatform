using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface IUserTokenService
    {
        Task<IEnumerable<UserTokenResponse>> GetAllAsync();
        Task<UserTokenResponse?> GetByIdAsync(int id);
        Task<UserTokenResponse> CreateAsync(UserTokenCreateRequest request);
        Task<UserTokenResponse?> UpdateAsync(int id, UserTokenUpdateRequest request);
        Task<bool> DeleteAsync(int id);
    }
}
