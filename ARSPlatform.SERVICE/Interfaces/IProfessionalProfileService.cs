using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface IProfessionalProfileService
    {
        Task<IEnumerable<ProfessionalProfileResponse>> GetAllAsync();
        Task<ProfessionalProfileResponse?> GetByIdAsync(int id);
        Task<ProfessionalProfileResponse> CreateAsync(ProfessionalProfileCreateRequest request);
        Task<ProfessionalProfileResponse?> UpdateAsync(int id, ProfessionalProfileUpdateRequest request);
        Task<ProfessionalProfileResponse?> UpdateAvailabilityAsync(int id, bool isAvailable);
        Task<bool> DeleteAsync(int id);
    }
}
