using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.REPO.PAGINATION;
using Microsoft.EntityFrameworkCore;

namespace ARSPlatform.REPOSITORIES
{
    public class AuditLogRepository
        : GenericRepository<AuditLog>,
          IAuditLogRepository
    {
        public AuditLogRepository(AppDbContext context)
            : base(context)
        {
        }

        public async Task<PagedResult<AuditLog>> GetPagedAsync(
            string? search,
            int? adminId,
            string? range,
            PaginationParams paginationParams)
        {
            var query = BuildQuery(search, adminId, range);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.Timestamp)
                .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
                .Take(paginationParams.PageSize)
                .AsNoTracking()
                .ToListAsync();

            return new PagedResult<AuditLog>(
                items,
                totalCount,
                paginationParams.PageNumber,
                paginationParams.PageSize);
        }

        public async Task<List<AuditLog>> GetForExportAsync(
            string? search,
            int? adminId,
            string? range)
        {
            return await BuildQuery(search, adminId, range)
                .OrderByDescending(x => x.Timestamp)
                .AsNoTracking()
                .ToListAsync();
        }

        private IQueryable<AuditLog> BuildQuery(
            string? search,
            int? adminId,
            string? range)
        {
            var query = _dbSet.AsQueryable();

            if (adminId.HasValue)
            {
                query = query.Where(x => x.AdminId == adminId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim();

                query = query.Where(x =>
                    x.AdminName.Contains(keyword) ||
                    x.Action.Contains(keyword) ||
                    x.Target.Contains(keyword) ||
                    (x.TargetId != null && x.TargetId.Contains(keyword)) ||
                    (x.Details != null && x.Details.Contains(keyword)));
            }

            var fromDate = GetRangeStart(range);

            if (fromDate.HasValue)
            {
                query = query.Where(x => x.Timestamp >= fromDate.Value);
            }

            return query;
        }

        private static DateTime? GetRangeStart(string? range)
        {
            if (string.IsNullOrWhiteSpace(range) ||
                range.Equals("all_time", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var now = DateTime.UtcNow;

            return range.ToLowerInvariant() switch
            {
                "past_24h" => now.AddHours(-24),
                "past_7d" => now.AddDays(-7),
                "past_30d" => now.AddDays(-30),
                _ => throw new ArgumentException(
                    "range must be one of: past_24h, past_7d, past_30d, all_time.")
            };
        }
    }
}