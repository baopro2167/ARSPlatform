using System.Linq;
using System.Threading.Tasks;
using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.REPO.PAGINATION;

namespace ARSPlatform.REPOSITORIES
{
    public class GuidanceProjectRepository : GenericRepository<GuidanceProject>, IGuidanceProjectRepository
    {
        public GuidanceProjectRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<PagedResult<GuidanceProject>> GetByLecturerIdPagedAsync(int lecturerId, PaginationParams paginationParams)
        {
            return await GetPagedAsync(
                paginationParams,
                predicate: x => x.LecturerId == lecturerId,
                orderBy: q => q.OrderBy(x => x.GuidanceProjectId),
                includes: new System.Linq.Expressions.Expression<System.Func<GuidanceProject, object>>[]
                {
                    x => x.Lecturer!,
                    x => x.Student!
                });
        }

        public async Task<PagedResult<GuidanceProject>> GetByLecturerIdPagedAsync(int lecturerId, int pageNumber, int pageSize)
        {
            return await GetByLecturerIdPagedAsync(lecturerId, new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
        }

        public async Task<PagedResult<GuidanceProject>> GetByStudentIdPagedAsync(int studentId, PaginationParams paginationParams)
        {
            return await GetPagedAsync(
                paginationParams,
                predicate: x => x.StudentId == studentId,
                orderBy: q => q.OrderBy(x => x.GuidanceProjectId),
                includes: new System.Linq.Expressions.Expression<System.Func<GuidanceProject, object>>[]
                {
                    x => x.Lecturer!,
                    x => x.Student!
                });
        }

        public async Task<PagedResult<GuidanceProject>> GetByStudentIdPagedAsync(int studentId, int pageNumber, int pageSize)
        {
            return await GetByStudentIdPagedAsync(studentId, new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
        }
    }
}
