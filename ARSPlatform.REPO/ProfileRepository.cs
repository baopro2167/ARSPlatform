using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ARSPlatform.REPOSITORIES
{
    public class ProfileRepository : GenericRepository<Profile>, IProfileRepository
    {
        public ProfileRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Profile>> GetAllWithUserAsync()
        {
            return await _dbSet
                .AsNoTracking()
                .Include(x => x.User)
                .OrderBy(x => x.UserId)
                .ToListAsync();
        }

        public async Task<Profile?> GetByIdWithUserAsync(int userId)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.UserId == userId);
        }
    }
}