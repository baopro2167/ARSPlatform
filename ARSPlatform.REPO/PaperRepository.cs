using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace ARSPlatform.REPOSITORIES
{
    public class PaperRepository : GenericRepository<Paper>, IPaperRepository
    {
        public PaperRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<Paper?> GetWithAuthorByIdAsync(Guid id)
        {
            return await _dbSet
                .Include(p => p.Author)
                .FirstOrDefaultAsync(p => p.Id == id);
        }
    }
}
