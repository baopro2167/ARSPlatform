using ARSPlatform.MODEL.Entities;

namespace ARSPlatform.REPO.Interfaces
{
    public interface ISeminarRepository : IGenericRepository<Seminar>
    {
        Task<IEnumerable<Seminar>> GetAllWithParticipantsAsync();
        Task<IEnumerable<Seminar>> GetAllForOrganizerWithParticipantsAsync(int organizerId);
        Task<Seminar?> GetByIdWithParticipantsAsync(int id);
        Task<IEnumerable<Seminar>> GetLifecycleCandidatesAsync();
        Task<IEnumerable<Seminar>> GetDueReminderSeminarsAsync(
            DateTime nowUtc,
            DateTime reminderCutoffUtc);
    }
}