using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ARSPlatform.REPOSITORIES
{
    public class MedalRepository : GenericRepository<Medal>, IMedalRepository
    {
        public MedalRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Medal>> GetAllWithFiltersAsync(string? role, string? tier, bool? isActive, string? search)
        {
            var query = _dbSet.AsNoTracking().AsQueryable();

            if (isActive.HasValue)
            {
                query = query.Where(m => m.IsActive == isActive.Value);
            }

            if (!string.IsNullOrWhiteSpace(tier) && !tier.Equals("ALL", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(m => m.Tier == tier);
            }

            if (!string.IsNullOrWhiteSpace(role) && !role.Equals("ALL", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(m => m.Roles.Contains("All") || m.Roles.Contains(role));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(m =>
                    m.Title.ToLower().Contains(s) ||
                    m.TitleVi.ToLower().Contains(s) ||
                    m.Code.ToLower().Contains(s) ||
                    m.CriteriaMetric.ToLower().Contains(s));
            }

            return await query
                .OrderBy(m => m.Roles)
                .ThenBy(m => m.StageLevel)
                .ThenBy(m => m.Tier)
                .ToListAsync();
        }

        public async Task<Medal?> GetByCodeAsync(string code)
        {
            return await _dbSet.FirstOrDefaultAsync(m => m.Code == code);
        }

        public async Task<bool> ExistsByCodeAsync(string code, string? excludeId = null)
        {
            return await _dbSet.AnyAsync(m => m.Code == code && (excludeId == null || m.Id != excludeId));
        }
    }
}
