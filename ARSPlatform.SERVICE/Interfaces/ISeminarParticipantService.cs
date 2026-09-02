using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface ISeminarParticipantService
    {
        Task<IEnumerable<SeminarParticipantResponse>> GetAllForOrganizerAsync(int organizerId);
        Task<IEnumerable<SeminarParticipantResponse>?> GetFeedbackBySeminarIdAsync(int seminarId, int organizerId);
        Task<PagedResult<SeminarParticipantResponse>> GetPagedForOrganizerAsync(PaginationParams paginationParams, int organizerId, int? seminarId = null);
        Task<PagedResult<SeminarParticipantResponse>> GetBySeminarIdAsync(int seminarId, int pageNumber, int pageSize);
        Task<PagedResult<SeminarParticipantResponse>> GetByUserIdAsync(int userId, int pageNumber, int pageSize);
        Task<PagedResult<SeminarParticipantResponse>> GetAllAsync(int pageNumber, int pageSize);
        Task<SeminarParticipantResponse?> GetByIdAsync(int id, int organizerId);
        Task<SeminarParticipantResponse> CreateAsync(SeminarParticipantCreateRequest request, int organizerId);
        Task<SeminarParticipantResponse?> UpdateAsync(int id, SeminarParticipantUpdateRequest request, int currentUserId);
        Task<bool> DeleteAsync(int id, int organizerId);
        Task<SeminarFeedbackResponse> SubmitFeedbackAsync(int seminarId, SeminarFeedbackRequest request, int currentUserId);
        Task<IEnumerable<SeminarInvitationResponse>> GetMyInvitationsAsync(int currentUserId);
    }
}