using System.Threading.Tasks;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.PAGINATION;

namespace ARSPlatform.REPO.Interfaces
{
    public interface IGroupMemberRepository : IGenericRepository<GroupMember>
    {
        Task<PagedResult<GroupMember>> GetByGroupIdPagedAsync(int groupId, PaginationParams paginationParams);
        Task<PagedResult<GroupMember>> GetByGroupIdPagedAsync(int groupId, int pageNumber, int pageSize);
        Task<PagedResult<GroupMember>> GetByStudentIdPagedAsync(int studentId, PaginationParams paginationParams);
        Task<PagedResult<GroupMember>> GetByStudentIdPagedAsync(int studentId, int pageNumber, int pageSize);
    }
}
