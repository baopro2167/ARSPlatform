using System.Linq;
using System.Threading.Tasks;
using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.REPO.PAGINATION;

namespace ARSPlatform.REPOSITORIES
{
    public class ReportRepository : GenericRepository<Report>, IReportRepository
    {
        public ReportRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<PagedResult<Report>> GetByReporterIdPagedAsync(int reporterId, PaginationParams paginationParams)
        {
            return await GetPagedAsync(
                paginationParams,
                predicate: x => x.ReporterId == reporterId,
                orderBy: q => q.OrderByDescending(x => x.CreatedAt),
                includes: new System.Linq.Expressions.Expression<System.Func<Report, object>>[]
                {
                    x => x.Reporter!
                });
        }

        public async Task<PagedResult<Report>> GetByReporterIdPagedAsync(int reporterId, int pageNumber, int pageSize)
        {
            return await GetByReporterIdPagedAsync(reporterId, new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
        }
    }
}
