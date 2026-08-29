using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface IPhasedReportService
    {
        Task<IEnumerable<PhasedReportResponse>> GetAllAsync(int? researchGroupId = null);
        Task<PagedResult<PhasedReportResponse>> GetPagedAsync(PaginationParams paginationParams, int? researchGroupId = null);
        Task<PagedResult<PhasedReportResponse>> GetByResearchGroupIdAsync(int researchGroupId, int pageNumber, int pageSize);
        Task<PagedResult<PhasedReportResponse>> GetByGroupMemberIdAsync(int groupMemberId, int pageNumber, int pageSize);
        Task<PagedResult<PhasedReportResponse>> GetAllAsync(int pageNumber, int pageSize);
        Task<PhasedReportResponse?> GetByIdAsync(int id);
        Task<PhasedReportResponse> CreateAsync(PhasedReportCreateRequest request, int? currentUserId = null);
        Task<PhasedReportResponse?> UpdateAsync(int id, PhasedReportUpdateRequest request, int? currentUserId = null);
        Task<bool> DeleteAsync(int id);
    }
}
