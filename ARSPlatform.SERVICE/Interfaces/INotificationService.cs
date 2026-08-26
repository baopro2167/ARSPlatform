using System.Collections.Generic;
using System.Threading.Tasks;
using ARSPlatform.REPO.PAGINATION;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface INotificationService
    {
        Task<IEnumerable<NotificationResponse>> GetAllAsync(int? userId = null);
        Task<PagedResult<NotificationResponse>> GetPagedAsync(PaginationParams paginationParams, int? userId = null);
        Task<PagedResult<NotificationResponse>> GetByUserIdAsync(int userId, int pageNumber, int pageSize);
        Task<PagedResult<NotificationResponse>> GetAllAsync(int pageNumber, int pageSize);
        Task<NotificationResponse?> GetByIdAsync(int id);
        Task<NotificationResponse> CreateAsync(NotificationCreateRequest request);
        Task<NotificationResponse?> UpdateAsync(int id, NotificationUpdateRequest request);
        Task<bool> DeleteAsync(int id);
    }
}
