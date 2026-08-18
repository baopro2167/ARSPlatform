using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ARSPlatform.REPOSITORIES
{
    public class ProfessionalProfileRepository : GenericRepository<ProfessionalProfile>, IProfessionalProfileRepository
    {
        public ProfessionalProfileRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<ProfessionalProfile>> GetAllWithUserAndFieldAsync()
        {
            return await _dbSet
                .AsNoTracking()
                .Include(x => x.User)
                .Include(x => x.SubField)
                    .ThenInclude(x => x!.MajorField)
                .OrderBy(x => x.UserId)
                .ToListAsync();
        }

        public async Task<ProfessionalProfile?> GetByIdWithUserAndFieldAsync(int userId)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(x => x.User)
                .Include(x => x.SubField)
                    .ThenInclude(x => x!.MajorField)
                .FirstOrDefaultAsync(x => x.UserId == userId);
        }
    }
}