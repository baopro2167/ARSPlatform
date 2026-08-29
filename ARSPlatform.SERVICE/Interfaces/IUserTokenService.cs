using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface IUserTokenService
    {
        Task<IEnumerable<UserTokenResponse>> GetAllAsync();
        Task<PagedResult<UserTokenResponse>> GetPagedAsync(PaginationParams paginationParams);
        Task<PagedResult<UserTokenResponse>> GetAllAsync(int pageNumber, int pageSize);
        Task<UserTokenResponse?> GetByIdAsync(int id);
        Task<UserTokenResponse> CreateAsync(UserTokenCreateRequest request);
        Task<UserTokenResponse?> UpdateAsync(int id, UserTokenUpdateRequest request);
        Task<bool> DeleteAsync(int id);
    }
}
