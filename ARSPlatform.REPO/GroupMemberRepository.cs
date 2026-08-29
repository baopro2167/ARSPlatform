using System.Linq;
using System.Threading.Tasks;
using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.REPO.PAGINATION;

namespace ARSPlatform.REPOSITORIES
{
    public class GroupMemberRepository : GenericRepository<GroupMember>, IGroupMemberRepository
    {
        public GroupMemberRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<PagedResult<GroupMember>> GetByGroupIdPagedAsync(int groupId, PaginationParams paginationParams)
        {
            return await GetPagedAsync(
                paginationParams,
                predicate: x => x.ResearchGroupId == groupId,
                orderBy: q => q.OrderBy(x => x.GroupMemberId),
                includes: new System.Linq.Expressions.Expression<System.Func<GroupMember, object>>[]
                {
                    x => x.Student!,
                    x => x.ResearchGroup!
                });
        }

        public async Task<PagedResult<GroupMember>> GetByGroupIdPagedAsync(int groupId, int pageNumber, int pageSize)
        {
            return await GetByGroupIdPagedAsync(groupId, new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
        }

        public async Task<PagedResult<GroupMember>> GetByStudentIdPagedAsync(int studentId, PaginationParams paginationParams)
        {
            return await GetPagedAsync(
                paginationParams,
                predicate: x => x.StudentId == studentId,
                orderBy: q => q.OrderBy(x => x.GroupMemberId),
                includes: new System.Linq.Expressions.Expression<System.Func<GroupMember, object>>[]
                {
                    x => x.Student!,
                    x => x.ResearchGroup!
                });
        }

        public async Task<PagedResult<GroupMember>> GetByStudentIdPagedAsync(int studentId, int pageNumber, int pageSize)
        {
            return await GetByStudentIdPagedAsync(studentId, new PaginationParams { PageNumber = pageNumber, PageSize = pageSize });
        }
    }
}
