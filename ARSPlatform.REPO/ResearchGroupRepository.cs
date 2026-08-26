using System.Linq;
using System.Threading.Tasks;
using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.REPO.PAGINATION;

namespace ARSPlatform.REPOSITORIES
{
    public class ResearchGroupRepository : GenericRepository<ResearchGroup>, IResearchGroupRepository
    {
        public ResearchGroupRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<PagedResult<ResearchGroup>> GetByLecturerIdPagedAsync(int lecturerId, PaginationParams paginationParams)
        {
            return await GetPagedAsync(
                paginationParams,
                predicate: x => x.LecturerId == lecturerId,
                orderBy: q => q.OrderBy(x => x.ResearchGroupId),
                includes: x => x.Topic!);
        }

        public async Task<PagedResult<ResearchGroup>> GetByLecturerIdPagedAsync(int lecturerId, int pageNumber, int pageSize)
        {
            return await GetByLecturerIdPagedAsync(lecturerId, new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
        }

        public async Task<PagedResult<ResearchGroup>> GetByTopicIdPagedAsync(int topicId, PaginationParams paginationParams)
        {
            return await GetPagedAsync(
                paginationParams,
                predicate: x => x.TopicId == topicId,
                orderBy: q => q.OrderBy(x => x.ResearchGroupId),
                includes: x => x.Lecturer!);
        }

        public async Task<PagedResult<ResearchGroup>> GetByTopicIdPagedAsync(int topicId, int pageNumber, int pageSize)
        {
            return await GetByTopicIdPagedAsync(topicId, new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
        }
    }
}
