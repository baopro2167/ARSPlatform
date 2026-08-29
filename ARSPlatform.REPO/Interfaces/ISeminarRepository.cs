using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.PAGINATION;

namespace ARSPlatform.REPO.Interfaces
{
    public interface ISeminarRepository : IGenericRepository<Seminar>
    {
        Task<IEnumerable<Seminar>> GetAllWithParticipantsAsync();
        Task<IEnumerable<Seminar>> GetAllForOrganizerWithParticipantsAsync(int organizerId);
        Task<PagedResult<Seminar>> GetByOrganizerIdPagedAsync(int organizerId, PaginationParams paginationParams);
        Task<PagedResult<Seminar>> GetByOrganizerIdPagedAsync(int organizerId, int pageNumber, int pageSize);
        Task<Seminar?> GetByIdWithParticipantsAsync(int id);
        Task<IEnumerable<Seminar>> GetLifecycleCandidatesAsync();
        Task<IEnumerable<Seminar>> GetDueReminderSeminarsAsync(
            DateTime nowUtc,
            DateTime reminderCutoffUtc);
    }
}