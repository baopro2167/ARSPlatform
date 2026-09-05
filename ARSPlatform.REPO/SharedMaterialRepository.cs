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

        public async Task<List<SharedMaterial>> GetFeedAsync(int userId, bool includeExpired = false, string? status = null, int? learningMaterialId = null)
        {
            var now = System.DateTime.UtcNow;
            var query = _context.SharedMaterials
                .AsNoTracking()
                .Include(x => x.Lecturer)
                .Include(x => x.SharedWithColleague)
                .Include(x => x.LearningMaterial)
                .Include(x => x.Paper)
                .Where(x => x.LecturerId == userId || x.SharedWithColleagueId == userId);

            if (learningMaterialId.HasValue && learningMaterialId.Value > 0)
            {
                query = query.Where(x => x.LearningMaterialId == learningMaterialId.Value || x.PaperId == learningMaterialId.Value);
            }

            if (!string.IsNullOrWhiteSpace(status) && !status.Equals("ALL", System.StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(x => x.Status != null && x.Status.ToUpper() == status.ToUpper());
            }

            var list = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
                query.OrderByDescending(x => x.SharedAt ?? x.CreatedAt ?? System.DateTime.MinValue));

            if (!includeExpired)
            {
                list = list.Where(x => x.ExpiresAt == null || x.ExpiresAt.Value > now).ToList();
            }

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
