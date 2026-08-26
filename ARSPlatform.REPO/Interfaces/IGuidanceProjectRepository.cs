using System.Threading.Tasks;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.PAGINATION;

namespace ARSPlatform.REPO.Interfaces
{
    public interface IGuidanceProjectRepository : IGenericRepository<GuidanceProject>
    {
        Task<PagedResult<GuidanceProject>> GetByLecturerIdPagedAsync(int lecturerId, PaginationParams paginationParams);
        Task<PagedResult<GuidanceProject>> GetByLecturerIdPagedAsync(int lecturerId, int pageNumber, int pageSize);
        Task<PagedResult<GuidanceProject>> GetByStudentIdPagedAsync(int studentId, PaginationParams paginationParams);
        Task<PagedResult<GuidanceProject>> GetByStudentIdPagedAsync(int studentId, int pageNumber, int pageSize);
    }
}
