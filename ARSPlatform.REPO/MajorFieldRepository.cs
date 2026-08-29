using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ARSPlatform.REPOSITORIES
{
    public class MajorFieldRepository : GenericRepository<MajorField>, IMajorFieldRepository
    {
        public MajorFieldRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<MajorField>> GetAllWithSubFieldsAsync()
        {
            return await _dbSet
                .AsNoTracking()
                .Include(x => x.SubFields)
                .OrderBy(x => x.MajorFieldId)
                .ToListAsync();
        }

        public async Task<MajorField?> GetByIdWithSubFieldsAsync(int id)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(x => x.SubFields)
                .FirstOrDefaultAsync(x => x.MajorFieldId == id);
        }

        public async Task<bool> HasSubFieldsAsync(int id)
        {
            return await _context.SubFields
                .AnyAsync(x => x.MajorFieldId == id);
        }
    }
}