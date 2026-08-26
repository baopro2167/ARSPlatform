using System.Linq;
using System.Threading.Tasks;
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
    }
}
