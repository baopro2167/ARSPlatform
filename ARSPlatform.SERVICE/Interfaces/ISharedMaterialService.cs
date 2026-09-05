using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface ISharedMaterialService
    {
        Task<IEnumerable<SharedMaterialResponse>> GetAllAsync();
        Task<PagedResult<SharedMaterialResponse>> GetPagedAsync(PaginationParams paginationParams);
        Task<PagedResult<SharedMaterialResponse>> GetByLecturerIdAsync(int lecturerId, int pageNumber, int pageSize);
        Task<PagedResult<SharedMaterialResponse>> GetByPaperIdAsync(int paperId, int pageNumber, int pageSize);
        Task<PagedResult<SharedMaterialResponse>> GetAllAsync(int pageNumber, int pageSize);
        Task<SharedMaterialResponse?> GetByIdAsync(int id);
        Task<SharedMaterialResponse?> GetByIdAsync(int id, int currentUserId);
        Task<SharedMaterialResponse> CreateAsync(SharedMaterialCreateRequest request);
        Task<SharedMaterialResponse> CreateShareAsync(SharedMaterialCreateRequest request, int currentUserId, bool isAdmin = false);
        Task<SharedMaterialResponse?> UpdateAsync(int id, SharedMaterialUpdateRequest request);
        Task<SharedMaterialResponse?> UpdateAsync(int id, SharedMaterialUpdateRequest request, int currentUserId, bool isAdmin = false);
        Task<SharedMaterialResponse?> UpdateStatusAsync(int id, string newStatus, int currentUserId, System.DateTime? respondedAt = null, bool isAdmin = false);
        Task<bool> DeleteAsync(int id);
        Task<bool> RevokeOrDeleteAsync(int id, int currentUserId, bool isAdmin = false);
        Task<List<SharedMaterialResponse>> GetFeedAsync(int currentUserId, bool includeExpired = false, string? status = null, int? learningMaterialId = null);
    }
}
