using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface IProfessionalProfileService
    {
        Task<IEnumerable<ProfessionalProfileResponse>> GetAllAsync();
        Task<PagedResult<ProfessionalProfileResponse>> GetPagedAsync(PaginationParams paginationParams);
        Task<PagedResult<ProfessionalProfileResponse>> GetAllAsync(int pageNumber, int pageSize);
        Task<ProfessionalProfileResponse?> GetByIdAsync(int id);
        Task<ProfessionalProfileResponse> CreateAsync(ProfessionalProfileCreateRequest request);
        Task<ProfessionalProfileResponse?> UpdateAsync(int id, ProfessionalProfileUpdateRequest request);
        Task<ProfessionalProfileResponse?> UpdateAvailabilityAsync(int id, bool isAvailable);
        Task<bool> DeleteAsync(int id);
    }
}
