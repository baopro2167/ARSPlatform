using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface IProfileService
    {
        Task<IEnumerable<ProfileResponse>> GetAllAsync();
        Task<PagedResult<ProfileResponse>> GetPagedAsync(PaginationParams paginationParams);
        Task<PagedResult<ProfileResponse>> GetAllAsync(int pageNumber, int pageSize);
        Task<ProfileResponse?> GetByIdAsync(int id);
        Task<ProfileResponse> CreateAsync(ProfileCreateRequest request);
        Task<ProfileResponse?> UpdateAsync(int id, ProfileUpdateRequest request);
        Task<bool> DeleteAsync(int id);
    }
}
