using System.Threading.Tasks;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.PAGINATION;

namespace ARSPlatform.REPO.Interfaces
{
    public interface INotificationRepository : IGenericRepository<Notification>
    {
        Task<PagedResult<Notification>> GetByUserIdPagedAsync(int userId, PaginationParams paginationParams);
        Task<PagedResult<Notification>> GetByUserIdPagedAsync(int userId, int pageNumber, int pageSize);
    }
}
