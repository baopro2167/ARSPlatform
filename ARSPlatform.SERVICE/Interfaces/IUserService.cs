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
        Task<PagedResult<UserResponse>> GetAllAsync(int pageNumber, int pageSize);
        Task<UserResponse?> GetUserByIdAsync(int id);
        Task<UserResponse?> UpdateUserAsync(int id, UserUpdateRequest request);
        Task<bool> DeleteUserAsync(int id);
    }
}
