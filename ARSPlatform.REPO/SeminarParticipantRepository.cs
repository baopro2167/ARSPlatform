using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
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

        public async Task<SeminarParticipant?>
            GetByIdWithSeminarAndUserAsync(int id)
        {
            return await _dbSet
                .Include(p => p.Seminar)
                .Include(p => p.User)
                .FirstOrDefaultAsync(
                    p => p.SeminarParticipantId == id);
        }
    }
}