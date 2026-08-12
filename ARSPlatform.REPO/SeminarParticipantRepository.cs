using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ARSPlatform.REPO
{
    public class SeminarParticipantRepository
        : ISeminarParticipantRepository
    {
        private readonly AppDbContext _context;

        public SeminarParticipantRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<SeminarParticipant>> GetAllAsync()
        {
            return await _context.SeminarParticipants
                .Include(sp => sp.Seminar)
                .Include(sp => sp.User)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<SeminarParticipant?> GetByIdAsync(
            int seminarParticipantId)
        {
            return await _context.SeminarParticipants
                .Include(sp => sp.Seminar)
                .Include(sp => sp.User)
                .FirstOrDefaultAsync(
                    sp => sp.SeminarParticipantId == seminarParticipantId);
        }

        public async Task<IEnumerable<SeminarParticipant>> GetBySeminarIdAsync(
            int seminarId)
        {
            return await _context.SeminarParticipants
                .Include(sp => sp.User)
                .Where(sp => sp.SeminarId == seminarId)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<SeminarParticipant> CreateAsync(
            SeminarParticipant seminarParticipant)
        {
            _context.SeminarParticipants.Add(seminarParticipant);

            await _context.SaveChangesAsync();

            return seminarParticipant;
        }

        public async Task<SeminarParticipant> UpdateAsync(
            SeminarParticipant seminarParticipant)
        {
            _context.SeminarParticipants.Update(seminarParticipant);

            await _context.SaveChangesAsync();

            return seminarParticipant;
        }

        public async Task<bool> DeleteAsync(int seminarParticipantId)
        {
            var participant = await _context.SeminarParticipants
                .FirstOrDefaultAsync(
                    sp => sp.SeminarParticipantId == seminarParticipantId);

            if (participant == null)
            {
                return false;
            }

            _context.SeminarParticipants.Remove(participant);

            await _context.SaveChangesAsync();

            return true;
        }
    }
}