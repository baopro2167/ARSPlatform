using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface ISeminarService
    {
        Task<IEnumerable<SeminarResponse>> GetAllAsync(int organizerId);
        Task<PagedResult<SeminarResponse>> GetPagedAsync(PaginationParams paginationParams, int organizerId);
        Task<PagedResult<SeminarResponse>> GetByOrganizerIdAsync(int organizerId, int pageNumber, int pageSize);
        Task<SeminarResponse?> GetByIdAsync(int seminarId, int? organizerId = null);
        Task<SeminarResponse?> GetByIdForViewerAsync(int seminarId, int currentUserId);
        Task<SeminarResponse> CreateAsync(int organizerId, SeminarCreateRequest request, CancellationToken cancellationToken = default);
        Task<SeminarResponse?> UpdateAsync(int seminarId, int organizerId, SeminarUpdateRequest request, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(int seminarId, int organizerId);
        Task<SeminarInviteResponse> InviteAsync(int seminarId, int organizerId, SeminarInviteRequest request, CancellationToken cancellationToken = default);
        Task<SeminarStatsResponse?> GetStatsAsync(int seminarId, int organizerId);
        Task<SeminarReminderResponse> SendFeedbackRemindersAsync(int seminarId, int organizerId, CancellationToken cancellationToken = default);
        Task<bool> IsOwnedByOrganizerAsync(int seminarId, int organizerId);
        Task UpdateLifecycleStatusesAsync(CancellationToken cancellationToken = default);
        Task SendDueEventRemindersAsync(CancellationToken cancellationToken = default);
        Task<List<SuggestedInviteeDto>> GetSuggestedInviteesAsync(int subFieldId, int currentUserId);
        Task<SeminarFeedbackAiSummaryResponse> SummarizeFeedbackAsync(int seminarId, int organizerId, CancellationToken cancellationToken = default);
    }
}