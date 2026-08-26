using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface IGuidanceProjectService
    {
        Task<IEnumerable<GuidanceProjectResponse>> GetAllAsync();
        Task<PagedResult<GuidanceProjectResponse>> GetPagedAsync(PaginationParams paginationParams);
        Task<PagedResult<GuidanceProjectResponse>> GetByLecturerIdAsync(int lecturerId, int pageNumber, int pageSize);
        Task<PagedResult<GuidanceProjectResponse>> GetByStudentIdAsync(int studentId, int pageNumber, int pageSize);
        Task<PagedResult<GuidanceProjectResponse>> GetAllAsync(int pageNumber, int pageSize);
        Task<GuidanceProjectResponse?> GetByIdAsync(int id);
        Task<GuidanceProjectResponse> CreateAsync(GuidanceProjectCreateRequest request);
        Task<GuidanceProjectResponse?> UpdateAsync(int id, GuidanceProjectUpdateRequest request);
        Task<bool> DeleteAsync(int id);
    }
}
