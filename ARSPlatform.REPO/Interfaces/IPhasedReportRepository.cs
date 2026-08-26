using System.Threading.Tasks;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.PAGINATION;

namespace ARSPlatform.REPO.Interfaces
{
    public interface IPhasedReportRepository : IGenericRepository<PhasedReport>
    {
        Task<PagedResult<PhasedReport>> GetByResearchGroupIdPagedAsync(int researchGroupId, PaginationParams paginationParams);
        Task<PagedResult<PhasedReport>> GetByResearchGroupIdPagedAsync(int researchGroupId, int pageNumber, int pageSize);
        Task<PagedResult<PhasedReport>> GetByGroupMemberIdPagedAsync(int groupMemberId, PaginationParams paginationParams);
        Task<PagedResult<PhasedReport>> GetByGroupMemberIdPagedAsync(int groupMemberId, int pageNumber, int pageSize);
    }
}
