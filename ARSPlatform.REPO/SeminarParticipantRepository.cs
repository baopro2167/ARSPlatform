using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.REPO.PAGINATION;
using Microsoft.EntityFrameworkCore;

namespace ARSPlatform.REPOSITORIES
{
    public class SeminarParticipantRepository : GenericRepository<SeminarParticipant>, ISeminarParticipantRepository
    {
        public SeminarParticipantRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<SeminarParticipant>> GetAllWithUserAsync()
        {
            return await _dbSet
                .AsNoTracking()
                .Include(p => p.User)
                .Include(p => p.Seminar)
                .ToListAsync();
        }

        public async Task<IEnumerable<SeminarParticipant>>
            GetAllForOrganizerWithUserAsync(int organizerId)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(p => p.User)
                .Include(p => p.Seminar)
                .Where(p =>
                    p.Seminar != null
                    && p.Seminar.OrganizerId == organizerId)
                .ToListAsync();
        }

        public async Task<IEnumerable<SeminarParticipant>>
            GetBySeminarIdWithUserAsync(int seminarId)
        {
            return await _dbSet
                .Include(p => p.User)
                .Where(p => p.SeminarId == seminarId)
                .ToListAsync();
        }

        public async Task<PagedResult<SeminarParticipant>> GetBySeminarIdPagedAsync(int seminarId, PaginationParams paginationParams)
        {
            return await GetPagedAsync(
                paginationParams,
                predicate: p => p.SeminarId == seminarId,
                orderBy: q => q.OrderBy(p => p.SeminarParticipantId),
                includes: new System.Linq.Expressions.Expression<System.Func<SeminarParticipant, object>>[]
                {
                    p => p.User!,
                    p => p.Seminar!
                });
        }

        public async Task<PagedResult<SeminarParticipant>> GetBySeminarIdPagedAsync(int seminarId, int pageNumber, int pageSize)
        {
            return await GetBySeminarIdPagedAsync(seminarId, new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
        }

        public async Task<PagedResult<SeminarParticipant>> GetByUserIdPagedAsync(int userId, PaginationParams paginationParams)
        {
            return await GetPagedAsync(
                paginationParams,
                predicate: p => p.UserId == userId,
                orderBy: q => q.OrderByDescending(p => p.SeminarParticipantId),
                includes: new System.Linq.Expressions.Expression<System.Func<SeminarParticipant, object>>[]
                {
                    p => p.Seminar!
                });
        }

        public async Task<PagedResult<SeminarParticipant>> GetByUserIdPagedAsync(int userId, int pageNumber, int pageSize)
        {
            return await GetByUserIdPagedAsync(userId, new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
        }

        public async Task<SeminarParticipant?>
            GetByIdWithSeminarAndUserAsync(int id)
        {
            return await _dbSet
                .Include(p => p.Seminar)
                    .ThenInclude(s => s!.Organizer)
                .Include(p => p.User)
                .FirstOrDefaultAsync(
                    p => p.SeminarParticipantId == id);
        }

        public async Task<SeminarParticipant?>
            GetBySeminarAndUserAsync(int seminarId, int userId, string? email = null)
        {
            return await _dbSet
                .Include(p => p.Seminar)
                    .ThenInclude(s => s!.Organizer)
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.SeminarId == seminarId &&
                    (p.UserId == userId || (!string.IsNullOrEmpty(email) && p.InvitedEmail == email)));
        }

        public async Task<IEnumerable<SeminarParticipant>>
            GetMyInvitationsAsync(int userId, string? email = null)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(p => p.Seminar)
                    .ThenInclude(s => s!.Organizer)
                .Include(p => p.User)
                .Where(p => p.UserId == userId || (!string.IsNullOrEmpty(email) && p.InvitedEmail == email))
                .OrderByDescending(p => p.Seminar != null ? p.Seminar.StartTime : System.DateTime.MinValue)
                .ToListAsync();
        }
    }
}