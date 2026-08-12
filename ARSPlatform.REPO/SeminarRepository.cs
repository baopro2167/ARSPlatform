using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ARSPlatform.REPO
{
    public class SeminarRepository : ISeminarRepository
    {
        private readonly AppDbContext _context;

        public SeminarRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Seminar>> GetAllAsync()
        {
            return await _context.Seminars
                .Include(s => s.Organizer)
                .Include(s => s.SeminarParticipants)
                    .ThenInclude(sp => sp.User)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Seminar?> GetByIdAsync(int seminarId)
        {
            return await _context.Seminars
                .Include(s => s.Organizer)
                .Include(s => s.SeminarParticipants)
                    .ThenInclude(sp => sp.User)
                .FirstOrDefaultAsync(s => s.SeminarId == seminarId);
        }

        public async Task<Seminar> CreateAsync(Seminar seminar)
        {
            _context.Seminars.Add(seminar);

            await _context.SaveChangesAsync();

            return seminar;
        }

        public async Task<Seminar> UpdateAsync(Seminar seminar)
        {
            _context.Seminars.Update(seminar);

            await _context.SaveChangesAsync();

            return seminar;
        }

        public async Task<bool> DeleteAsync(int seminarId)
        {
            var seminar = await _context.Seminars
                .FirstOrDefaultAsync(s => s.SeminarId == seminarId);

            if (seminar == null)
            {
                return false;
            }

            _context.Seminars.Remove(seminar);

            await _context.SaveChangesAsync();

            return true;
        }
    }
}