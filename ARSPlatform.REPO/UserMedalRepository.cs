using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        public async Task<List<UserMedal>> GetAllByUserIdAsync(int userId)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(um => um.Medal)
                .Where(um => um.UserId == userId)
                .ToListAsync();
        }

        public async Task<int> CountUnlockedAsync()
        {
            return await _dbSet.CountAsync(um => um.IsUnlocked);
        }

        public async Task<int> CountUnlockedByMedalIdAsync(string medalId)
        {
            return await _dbSet.CountAsync(um => um.MedalId == medalId && um.IsUnlocked);
        }

        public async Task<List<UserMedal>> GetLeaderboardAsync(string medalId, int topN)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(um => um.User)
                .Where(um => um.MedalId == medalId)
                .OrderByDescending(um => um.CurrentProgress)
                .Take(topN)
                .ToListAsync();
        }
    }
}
