using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.REPO.PAGINATION;
using Microsoft.EntityFrameworkCore;

namespace ARSPlatform.REPOSITORIES
{
    public class SubFieldRepository : GenericRepository<SubField>, ISubFieldRepository
    {
        public SubFieldRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<SubField>> GetAllWithMajorFieldAsync(int? majorFieldId = null)
        {
            var query = _dbSet
                .AsNoTracking()
                .Include(x => x.MajorField)
                .AsQueryable();

            if (majorFieldId.HasValue)
            {
                query = query.Where(x => x.MajorFieldId == majorFieldId.Value);
            }

            return await query
                .OrderBy(x => x.MajorFieldId)
                .ThenBy(x => x.SubFieldId)
                .ToListAsync();
        }
        public async Task<PagedResult<SubField>> GetByMajorFieldIdPagedAsync(int majorFieldId, PaginationParams paginationParams)
        {
            return await GetPagedAsync(
                paginationParams,
                predicate: x => x.MajorFieldId == majorFieldId,
                orderBy: q => q.OrderBy(x => x.SubFieldId),
                includes: x => x.MajorField!);
        }

        public async Task<PagedResult<SubField>> GetByMajorFieldIdPagedAsync(int majorFieldId, int pageNumber, int pageSize)
        {
            return await GetByMajorFieldIdPagedAsync(majorFieldId, new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
        }

        public async Task<SubField?> GetByIdWithMajorFieldAsync(int id)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(x => x.MajorField)
                .FirstOrDefaultAsync(x => x.SubFieldId == id);
        }

        public async Task<bool> HasUsageAsync(int id)
        {
            return await _context.ProfessionalProfiles.AnyAsync(x => x.SubFieldId == id)
                || await _context.Papers.AnyAsync(x => x.SubFieldId == id)
                || await _context.LearningMaterials.AnyAsync(x => x.SubFieldId == id);
        }
    }
}