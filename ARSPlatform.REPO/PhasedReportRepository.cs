using System.Linq;
using System.Threading.Tasks;
using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.REPO.PAGINATION;

namespace ARSPlatform.REPOSITORIES
{
    public class PhasedReportRepository : GenericRepository<PhasedReport>, IPhasedReportRepository
    {
        public PhasedReportRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<PagedResult<PhasedReport>> GetByResearchGroupIdPagedAsync(int researchGroupId, PaginationParams paginationParams)
        {
            return await GetPagedAsync(
                paginationParams,
                predicate: x => x.ResearchGroupId == researchGroupId,
                orderBy: q => q.OrderByDescending(x => x.SubmittedAt),
                includes: new System.Linq.Expressions.Expression<System.Func<PhasedReport, object>>[]
                {
                    x => x.GroupMember!
                });
        }

        public async Task<PagedResult<PhasedReport>> GetByResearchGroupIdPagedAsync(int researchGroupId, int pageNumber, int pageSize)
        {
            return await GetByResearchGroupIdPagedAsync(researchGroupId, new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
        }

        public async Task<PagedResult<PhasedReport>> GetByGroupMemberIdPagedAsync(int groupMemberId, PaginationParams paginationParams)
        {
            return await GetPagedAsync(
                paginationParams,
                predicate: x => x.GroupMemberId == groupMemberId,
                orderBy: q => q.OrderByDescending(x => x.SubmittedAt),
                includes: new System.Linq.Expressions.Expression<System.Func<PhasedReport, object>>[]
                {
                    x => x.ResearchGroup!
                });
        }

        public async Task<PagedResult<PhasedReport>> GetByGroupMemberIdPagedAsync(int groupMemberId, int pageNumber, int pageSize)
        {
            return await GetByGroupMemberIdPagedAsync(groupMemberId, new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
        }
    }
}
