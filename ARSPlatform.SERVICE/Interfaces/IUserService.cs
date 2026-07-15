using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using System;
using System.Threading.Tasks;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface IUserService
    {
        Task<PagedResult<UserResponse>> GetUsersAsync(PaginationParams paginationParams);
        Task<UserResponse?> GetUserByIdAsync(Guid id);
        Task<UserResponse?> UpdateUserAsync(Guid id, UserUpdateRequest request);
        Task<bool> DeleteUserAsync(Guid id);
    }
}
