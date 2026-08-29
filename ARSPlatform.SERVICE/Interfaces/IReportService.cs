using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface IReportService
    {
        Task<IEnumerable<ReportResponse>> GetAllAsync();
        Task<PagedResult<ReportResponse>> GetPagedAsync(PaginationParams paginationParams);
        Task<PagedResult<ReportResponse>> GetByReporterIdAsync(int reporterId, int pageNumber, int pageSize);
        Task<PagedResult<ReportResponse>> GetAllAsync(int pageNumber, int pageSize);
        Task<ReportResponse?> GetByIdAsync(int id);
        Task<ReportResponse> CreateAsync(ReportCreateRequest request);
        Task<ReportResponse?> UpdateAsync(int id, ReportUpdateRequest request);
        Task<bool> DeleteAsync(int id);
    }
}
