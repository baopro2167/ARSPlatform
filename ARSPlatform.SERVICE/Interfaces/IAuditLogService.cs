using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface IAuditLogService
    {
        Task<PagedResult<AuditLogResponse>> GetPagedAsync(string? search, int? adminId, string? range, PaginationParams? paginationParams);
        Task<byte[]> ExportCsvAsync(string? search, int? adminId, string? range);
        Task<AuditLogResponse> CreateAsync(AuditLogCreateRequest request);
    }
}
