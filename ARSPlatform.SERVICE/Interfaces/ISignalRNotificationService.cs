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
        /// Gửi cập nhật số lượng thông báo chưa đọc tới người dùng.
        /// </summary>
        Task SendUpdateUnreadCountAsync(int userId, int unreadCount);

        /// <summary>
        /// Gửi cập nhật trạng thái bài báo real-time tới tác giả, quản trị viên và người đang xem bài báo.
        /// </summary>
        Task SendPaperStatusUpdatedAsync(int paperId, string? status, string? authorshipStatus, int authorId);

        /// <summary>
        /// Gửi thông báo phân công phản biện tới Reviewer được gán.
        /// </summary>
        Task SendReviewRequestAssignedAsync(int reviewerId, int reviewRequestId, int paperId, string paperTitle, System.DateTime? deadline);

        /// <summary>
        /// Gửi cập nhật yêu cầu tham gia nhóm nghiên cứu tới Workspace của nhóm và ứng viên.
        /// </summary>
        Task SendGroupJoinRequestUpdatedAsync(int groupId, int requestId, string status, int applicantUserId, string applicantName);

        /// <summary>
        /// Gửi bình luận mới trên bài viết diễn đàn tới nhóm người đang xem bài viết.
        /// </summary>
        Task SendForumCommentAddedAsync(int forumPostId, int forumCommentId, int userId, string authorName, string content, System.DateTime createdAt);

        /// <summary>
        /// Gửi thông báo mở khóa huy hiệu (Medal) tới người dùng.
        /// </summary>
        Task SendMedalAwardedAsync(int userId, string medalId, string medalName, string? iconUrl, System.DateTime unlockedAt);
    }
}
