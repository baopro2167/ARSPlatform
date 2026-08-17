using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.PAGINATION;

namespace ARSPlatform.REPO.Interfaces
{
    public interface IAuditLogRepository : IGenericRepository<AuditLog>
    {
        Task<PagedResult<AuditLog>> GetPagedAsync(
            string? search,
            int? adminId,
            string? range,
            PaginationParams paginationParams);

        Task<List<AuditLog>> GetForExportAsync(
            string? search,
            int? adminId,
            string? range);
    }
}