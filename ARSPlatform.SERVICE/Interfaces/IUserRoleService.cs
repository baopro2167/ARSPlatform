using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface IUserRoleService
    {
        Task<IEnumerable<UserRoleResponse>> GetAllAsync();
        Task<UserRoleResponse?> GetByIdAsync(int id);
        Task<UserRoleResponse> CreateAsync(UserRoleCreateRequest request);
        Task<UserRoleResponse?> UpdateAsync(int id, UserRoleUpdateRequest request);
        Task<bool> DeleteAsync(int id);
    }
}
