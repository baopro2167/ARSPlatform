using System.Linq;
using System.Threading.Tasks;
using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.REPO.PAGINATION;

namespace ARSPlatform.REPOSITORIES
{
    public class LearningMaterialRepository : GenericRepository<LearningMaterial>, ILearningMaterialRepository
    {
        public LearningMaterialRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<PagedResult<LearningMaterial>> GetByLecturerIdPagedAsync(int lecturerId, PaginationParams paginationParams)
        {
            return await GetPagedAsync(
                paginationParams,
                predicate: x => x.LecturerId == lecturerId,
                orderBy: q => q.OrderBy(x => x.LearningMaterialId),
                includes: new System.Linq.Expressions.Expression<System.Func<LearningMaterial, object>>[]
                {
                    x => x.Lecturer!,
                    x => x.SubField!
                });
        }

        public async Task<PagedResult<LearningMaterial>> GetByLecturerIdPagedAsync(int lecturerId, int pageNumber, int pageSize)
        {
            return await GetByLecturerIdPagedAsync(lecturerId, new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
        }

        public async Task<PagedResult<LearningMaterial>> GetBySubFieldIdPagedAsync(int subFieldId, PaginationParams paginationParams)
        {
            return await GetPagedAsync(
                paginationParams,
                predicate: x => x.SubFieldId == subFieldId,
                orderBy: q => q.OrderBy(x => x.LearningMaterialId),
                includes: new System.Linq.Expressions.Expression<System.Func<LearningMaterial, object>>[]
                {
                    x => x.Lecturer!,
                    x => x.SubField!
                });
        }

        public async Task<PagedResult<LearningMaterial>> GetBySubFieldIdPagedAsync(int subFieldId, int pageNumber, int pageSize)
        {
            return await GetBySubFieldIdPagedAsync(subFieldId, new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
        }
    }
}
