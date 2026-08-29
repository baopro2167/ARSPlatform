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
        Task<NotificationResponse?> GetByIdAsync(int id, int? currentUserId = null, bool isAdmin = false);
        Task<int> GetUnreadCountAsync(int userId);
        Task<NotificationResponse?> MarkAsReadAsync(int id, int currentUserId, bool isAdmin = false);
        Task<int> MarkAllAsReadAsync(int userId);
        Task<NotificationResponse> CreateAsync(NotificationCreateRequest request);
        Task<NotificationResponse> CreateNotificationAsync(int userId, string message);
        Task<NotificationResponse?> UpdateAsync(int id, NotificationUpdateRequest request, int? currentUserId = null, bool isAdmin = false);
        Task<bool> DeleteAsync(int id, int? currentUserId = null, bool isAdmin = false);
    }
}
