using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.REPO.PAGINATION;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
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

        public async Task<PagedResult<Paper>> GetByAuthorIdPagedAsync(int authorId, PaginationParams paginationParams)
        {
            return await GetPagedAsync(
                paginationParams,
                predicate: x => x.CreatorId == authorId,
                orderBy: q => q.OrderByDescending(x => x.CreatedAt),
                includes: new System.Linq.Expressions.Expression<System.Func<Paper, object>>[]
                {
                    x => x.Creator!,
                    x => x.SubField!
                });
        }

        public async Task<PagedResult<Paper>> GetByAuthorIdPagedAsync(int authorId, int pageNumber, int pageSize)
        {
            return await GetByAuthorIdPagedAsync(authorId, new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
        }

        public async Task<PagedResult<Paper>> GetBySubFieldIdPagedAsync(int subFieldId, PaginationParams paginationParams)
        {
            return await GetPagedAsync(
                paginationParams,
                predicate: x => x.SubFieldId == subFieldId,
                orderBy: q => q.OrderByDescending(x => x.CreatedAt),
                includes: new System.Linq.Expressions.Expression<System.Func<Paper, object>>[]
                {
                    x => x.Creator!,
                    x => x.SubField!
                });
        }

        public async Task<PagedResult<Paper>> GetBySubFieldIdPagedAsync(int subFieldId, int pageNumber, int pageSize)
        {
            return await GetBySubFieldIdPagedAsync(subFieldId, new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
        }
    }
}
