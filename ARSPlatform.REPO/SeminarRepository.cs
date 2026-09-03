using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.REPO.PAGINATION;
using Microsoft.EntityFrameworkCore;

namespace ARSPlatform.REPOSITORIES
{
    public class SeminarRepository : GenericRepository<Seminar>, ISeminarRepository
    {
        public SeminarRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Seminar>> GetAllWithParticipantsAsync()
        {
            return await _dbSet
                .AsNoTracking()
                .Include(s => s.SubField)
                .Include(s => s.SeminarParticipants)
                    .ThenInclude(p => p.User)
                .OrderByDescending(s => s.StartTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<Seminar>> GetAllForOrganizerWithParticipantsAsync(int organizerId)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(s => s.SubField)
                .Include(s => s.SeminarParticipants)
                    .ThenInclude(p => p.User)
                .Where(s => s.OrganizerId == organizerId)
                .OrderByDescending(s => s.StartTime)
                .ToListAsync();
        }

        public async Task<PagedResult<Seminar>> GetByOrganizerIdPagedAsync(int organizerId, PaginationParams paginationParams)
        {
            return await GetPagedAsync(
                paginationParams,
                predicate: s => s.OrganizerId == organizerId,
                orderBy: q => q.OrderByDescending(s => s.StartTime),
                includes: new System.Linq.Expressions.Expression<System.Func<Seminar, object>>[]
                {
                    s => s.SubField!,
                    s => s.SeminarParticipants
                });
        }

        public async Task<PagedResult<Seminar>> GetByOrganizerIdPagedAsync(int organizerId, int pageNumber, int pageSize)
        {
            return await GetByOrganizerIdPagedAsync(organizerId, new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
        }

        public async Task<Seminar?> GetByIdWithParticipantsAsync(int id)
        {
            return await _dbSet
                .Include(s => s.SubField)
                .Include(s => s.SeminarParticipants)
                    .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(s => s.SeminarId == id);
        }

        public async Task<IEnumerable<Seminar>> GetLifecycleCandidatesAsync()
        {
            return await _dbSet
                .Where(s => s.Status == null || s.Status != "Draft")
                .ToListAsync();
        }

        public async Task<IEnumerable<Seminar>> GetDueReminderSeminarsAsync(
            DateTime nowUtc,
            DateTime reminderCutoffUtc)
        {
            return await _dbSet
                .Include(s => s.SeminarParticipants)
                    .ThenInclude(p => p.User)
                .Where(s =>
                    s.ReminderEnabled
                    && s.ReminderSentAt == null
                    && s.StartTime > nowUtc
                    && s.StartTime <= reminderCutoffUtc)
                .ToListAsync();
        }
    }
}