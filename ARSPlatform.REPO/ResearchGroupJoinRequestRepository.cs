using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;

namespace ARSPlatform.REPOSITORIES
{
    public class ResearchGroupJoinRequestRepository : GenericRepository<ResearchGroupJoinRequest>, IResearchGroupJoinRequestRepository
    {
        public ResearchGroupJoinRequestRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<ResearchGroupJoinRequest?> GetWithDetailsAsync(int joinRequestId, int? groupId = null)
        {
            var query = _dbSet
                .Include(r => r.ResearchGroup)
                .Include(r => r.DecidedByUser)
                .Include(r => r.ApplicantUser).ThenInclude(u => u.Profile)
                .Include(r => r.ApplicantUser).ThenInclude(u => u.ProfessionalProfile).ThenInclude(pp => pp.SubField)
                .AsQueryable();

            if (groupId.HasValue)
            {
                query = query.Where(r => r.ResearchGroupId == groupId.Value);
            }

            return await query.FirstOrDefaultAsync(r => r.JoinRequestId == joinRequestId);
        }

        public async Task<bool> HasPendingRequestAsync(int userId, int groupId)
        {
            return await _dbSet.AnyAsync(r => r.ResearchGroupId == groupId && r.ApplicantUserId == userId && r.Status == "PENDING");
        }

        public async Task<IEnumerable<ResearchGroupJoinRequest>> GetListWithDetailsAsync(int groupId, string? status)
        {
            var query = _dbSet
                .Include(r => r.ResearchGroup)
                .Include(r => r.DecidedByUser)
                .Include(r => r.ApplicantUser).ThenInclude(u => u.Profile)
                .Include(r => r.ApplicantUser).ThenInclude(u => u.ProfessionalProfile).ThenInclude(pp => pp.SubField)
                .Where(r => r.ResearchGroupId == groupId);

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(r => r.Status == status);
            }

            return await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
        }
    }
}
