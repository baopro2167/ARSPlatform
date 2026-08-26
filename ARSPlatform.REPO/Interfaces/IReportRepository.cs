using System.Threading.Tasks;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.PAGINATION;

namespace ARSPlatform.REPO.Interfaces
{
    public interface IReportRepository : IGenericRepository<Report>
    {
        Task<PagedResult<Report>> GetByReporterIdPagedAsync(int reporterId, PaginationParams paginationParams);
        Task<PagedResult<Report>> GetByReporterIdPagedAsync(int reporterId, int pageNumber, int pageSize);
    }
}
