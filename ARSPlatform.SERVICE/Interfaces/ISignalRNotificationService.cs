using System.Threading.Tasks;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Interfaces
{
    public interface ISignalRNotificationService
    {
        /// <summary>
        /// Gửi thông báo real-time tới 1 người dùng cụ thể.
        /// </summary>
        Task SendNotificationToUserAsync(int userId, NotificationResponse notification);

        /// <summary>
        /// Gửi cập nhật trạng thái bài báo real-time tới tác giả và quản trị viên.
        /// </summary>
        Task SendPaperStatusUpdatedAsync(int paperId, string? status, string? authorshipStatus, int authorId);
    }
}
