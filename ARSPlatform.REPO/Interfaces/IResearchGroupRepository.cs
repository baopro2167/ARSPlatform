using System.Threading.Tasks;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.PAGINATION;

namespace ARSPlatform.REPO.Interfaces
{
    public interface IResearchGroupRepository : IGenericRepository<ResearchGroup>
    {
        Task<PagedResult<ResearchGroup>> GetByLecturerIdPagedAsync(int lecturerId, PaginationParams paginationParams);
        Task<PagedResult<ResearchGroup>> GetByLecturerIdPagedAsync(int lecturerId, int pageNumber, int pageSize);
        Task<PagedResult<ResearchGroup>> GetByTopicIdPagedAsync(int topicId, PaginationParams paginationParams);
        Task<PagedResult<ResearchGroup>> GetByTopicIdPagedAsync(int topicId, int pageNumber, int pageSize);
    }
}
