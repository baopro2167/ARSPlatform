using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface IPhasedReportService
    {
        Task<IEnumerable<PhasedReportResponse>> GetAllAsync();
        Task<PagedResult<PhasedReportResponse>> GetPagedAsync(PaginationParams paginationParams);
        Task<PagedResult<PhasedReportResponse>> GetByResearchGroupIdAsync(int researchGroupId, int pageNumber, int pageSize);
        Task<PagedResult<PhasedReportResponse>> GetByGroupMemberIdAsync(int groupMemberId, int pageNumber, int pageSize);
        Task<PagedResult<PhasedReportResponse>> GetAllAsync(int pageNumber, int pageSize);
        Task<PhasedReportResponse?> GetByIdAsync(int id);
        Task<PhasedReportResponse> CreateAsync(PhasedReportCreateRequest request);
        Task<PhasedReportResponse?> UpdateAsync(int id, PhasedReportUpdateRequest request);
        Task<bool> DeleteAsync(int id);
    }
}
