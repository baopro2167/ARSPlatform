using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.REPO.PAGINATION;

namespace ARSPlatform.REPOSITORIES
{
    public class SharedMaterialRepository : GenericRepository<SharedMaterial>, ISharedMaterialRepository
    {
        public SharedMaterialRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<PagedResult<SharedMaterial>> GetByLecturerIdPagedAsync(int lecturerId, PaginationParams paginationParams)
        {
            return await GetPagedAsync(
                paginationParams,
                predicate: x => x.LecturerId == lecturerId,
                orderBy: q => q.OrderBy(x => x.SharedMaterialId),
                includes: new System.Linq.Expressions.Expression<System.Func<SharedMaterial, object>>[]
                {
                    x => x.Lecturer!,
                    x => x.Paper!,
                    x => x.SharedWithColleague!
                });
        }

        public async Task<PagedResult<SharedMaterial>> GetByLecturerIdPagedAsync(int lecturerId, int pageNumber, int pageSize)
        {
            return await GetByLecturerIdPagedAsync(lecturerId, new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
        }

        public async Task<PagedResult<SharedMaterial>> GetByPaperIdPagedAsync(int paperId, PaginationParams paginationParams)
        {
            return await GetPagedAsync(
                paginationParams,
                predicate: x => x.PaperId == paperId,
                orderBy: q => q.OrderBy(x => x.SharedMaterialId),
                includes: new System.Linq.Expressions.Expression<System.Func<SharedMaterial, object>>[]
                {
                    x => x.Lecturer!,
                    x => x.Paper!,
                    x => x.SharedWithColleague!
                });
        }

        public async Task<PagedResult<SharedMaterial>> GetByPaperIdPagedAsync(int paperId, int pageNumber, int pageSize)
        {
            return await GetByPaperIdPagedAsync(paperId, new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
        }

        public async Task<List<SharedMaterial>> GetFeedAsync(int userId, bool includeExpired = false, string? status = null, int? learningMaterialId = null, string? role = null)
        {
            var now = System.DateTime.UtcNow;
            var query = _context.SharedMaterials
                .AsNoTracking()
                .Include(x => x.Lecturer)
                .Include(x => x.SharedWithColleague)
                .Include(x => x.LearningMaterial)
                .Include(x => x.Paper)
                .AsQueryable();

            // Filter by direction / role if specified
            if (!string.IsNullOrWhiteSpace(role))
            {
                var roleLower = role.Trim().ToLowerInvariant();
                if (roleLower is "recipient" or "receiver" or "inbound")
                {
                    query = query.Where(x => x.SharedWithColleagueId == userId);
                }
                else if (roleLower is "sender" or "creator" or "outbound")
                {
                    query = query.Where(x => x.LecturerId == userId);
                }
                else
                {
                    query = query.Where(x => x.LecturerId == userId || x.SharedWithColleagueId == userId);
                }
            }
            else
            {
                query = query.Where(x => x.LecturerId == userId || x.SharedWithColleagueId == userId);
            }

            // Exclude orphaned records where underlying material no longer exists
            query = query.Where(x => x.LearningMaterial != null || x.Paper != null);

            // Filter by specific learning material ID if requested
            if (learningMaterialId.HasValue && learningMaterialId.Value > 0)
            {
                query = query.Where(x => x.LearningMaterialId == learningMaterialId.Value || x.PaperId == learningMaterialId.Value);
            }

            // Status and expiration filtering
            if (!string.IsNullOrWhiteSpace(status) && !status.Equals("ALL", System.StringComparison.OrdinalIgnoreCase))
            {
                var statusUpper = status.Trim().ToUpperInvariant();
                query = query.Where(x => x.Status != null && x.Status.ToUpper() == statusUpper);
                if (!includeExpired)
                {
                    query = query.Where(x => x.ExpiresAt == null || x.ExpiresAt.Value > now);
                }
            }
            else if (string.Equals(status, "ALL", System.StringComparison.OrdinalIgnoreCase))
            {
                if (!includeExpired)
                {
                    query = query.Where(x => x.ExpiresAt == null || x.ExpiresAt.Value > now);
                }
            }
            else
            {
                // Default feed: Exclude ENDED, DECLINED, REVOKED, CANCELLED, and EXPIRED records unless includeExpired is true
                if (!includeExpired)
                {
                    var terminalStatuses = new[] { "ENDED", "DECLINED", "REVOKED", "CANCELLED", "EXPIRED" };
                    query = query.Where(x => x.Status == null || !terminalStatuses.Contains(x.Status.ToUpper()));
                    query = query.Where(x => x.ExpiresAt == null || x.ExpiresAt.Value > now);
                }
            }

            var list = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
                query.OrderByDescending(x => x.SharedAt ?? x.CreatedAt ?? System.DateTime.MinValue));

            return list;
        }

        public async Task<SharedMaterial?> GetWithDetailsByIdAsync(int id)
        {
            return await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
                _context.SharedMaterials
                    .Include(x => x.Lecturer)
                    .Include(x => x.SharedWithColleague)
                    .Include(x => x.LearningMaterial)
                    .Include(x => x.Paper),
                x => x.SharedMaterialId == id);
        }

        public async Task<SharedMaterial?> FindPendingDuplicateAsync(int lecturerId, int colleagueId, int? learningMaterialId, int? paperId)
        {
            var now = System.DateTime.UtcNow;
            return await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
                _context.SharedMaterials.AsNoTracking(),
                x =>
                    x.LecturerId == lecturerId &&
                    x.SharedWithColleagueId == colleagueId &&
                    (
                        (learningMaterialId.HasValue && x.LearningMaterialId == learningMaterialId.Value) ||
                        (paperId.HasValue && x.PaperId == paperId.Value) ||
                        (learningMaterialId.HasValue && x.PaperId == learningMaterialId.Value) ||
                        (paperId.HasValue && x.LearningMaterialId == paperId.Value)
                    ) &&
                    (x.Status == "PENDING" || x.Status == "Pending" || x.Status == "pending") &&
                    (x.ExpiresAt == null || x.ExpiresAt > now));
        }
    }
}
