using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface ISeminarParticipantService
    {
        Task<IEnumerable<SeminarParticipantResponse>> GetAllForOrganizerAsync(int organizerId);
        Task<SeminarParticipantResponse?> GetByIdAsync(int id, int organizerId);
        Task<SeminarParticipantResponse> CreateAsync(SeminarParticipantCreateRequest request, int organizerId);
        Task<SeminarParticipantResponse?> UpdateAsync(int id, SeminarParticipantUpdateRequest request, int organizerId);
        Task<bool> DeleteAsync(int id, int organizerId);
    }
}
