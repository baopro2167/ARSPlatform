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

        public async Task<Paper?> GetWithAuthorByIdAsync(int id)
        {
            return await _dbSet
                .Include(p => p.Creator)
                .FirstOrDefaultAsync(p => p.PaperId == id);
        }
    }
}
