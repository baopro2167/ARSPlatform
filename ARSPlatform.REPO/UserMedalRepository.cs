using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ARSPlatform.REPOSITORIES
{
    public class UserMedalRepository : GenericRepository<UserMedal>, IUserMedalRepository
    {
        public UserMedalRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<UserMedal>> GetByUserIdWithMedalsAsync(int userId)
        {
            return await _dbSet
                .Include(um => um.Medal)
                .Where(um => um.UserId == userId)
                .ToListAsync();
        }

        public async Task<IEnumerable<UserMedal>> GetUnlockedByUserIdAsync(int userId)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(um => um.Medal)
                .Where(um => um.UserId == userId && um.IsUnlocked && um.Medal.IsActive)
                .OrderByDescending(um => um.UnlockedAt)
                .ToListAsync();
        }

        public async Task<UserMedal?> GetByUserAndMedalIdAsync(int userId, string medalId)
        {
            return await _dbSet
                .Include(um => um.Medal)
                .FirstOrDefaultAsync(um => um.UserId == userId && um.MedalId == medalId);
        }
    }
}
