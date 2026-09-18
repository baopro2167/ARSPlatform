using System;
using System.Threading.Tasks;
using ARSPlatform.API.Hubs;
using ARSPlatform.SERVICE.DTOs.Response;
using ARSPlatform.SERVICE.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace ARSPlatform.API.Services
{
    /// <summary>
    /// Service trung gian kết nối giữa Service Layer và SignalR NotificationHub
    /// </summary>
    public class SignalRNotificationService : ISignalRNotificationService
    {
        private readonly IHubContext<NotificationHub> _hubContext;

        public SignalRNotificationService(IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task SendNotificationToUserAsync(int userId, NotificationResponse notification)
        {
            try
            {
                // 1. Gửi tới client theo UserIdentifier
                await _hubContext.Clients.User(userId.ToString())
                    .SendAsync("ReceiveNotification", notification);

                // 2. Gửi tới Group User_{userId} theo yêu cầu
                await _hubContext.Clients.Group($"User_{userId}")
                    .SendAsync("ReceiveNotification", notification);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SignalR] Error sending notification to user {userId}: {ex.Message}");
            }
        }

        public async Task SendUpdateUnreadCountAsync(int userId, int unreadCount)
        {
            try
            {
                var userIdStr = userId.ToString();
                await _hubContext.Clients.User(userIdStr)
                    .SendAsync("UpdateUnreadCount", unreadCount);
                await _hubContext.Clients.Group($"User_{userId}")
                    .SendAsync("UpdateUnreadCount", unreadCount);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SignalR] Error sending unread count to user {userId}: {ex.Message}");
            }
        }

        public async Task SendPaperStatusUpdatedAsync(int paperId, string? status, string? authorshipStatus, int authorId)
        {
            try
            {
                var payload = new
                {
                    paperId,
                    status,
                    authorshipVerificationStatus = authorshipStatus
                };

                // 1. Gửi tới tác giả bài báo
                await _hubContext.Clients.User(authorId.ToString())
                    .SendAsync("PaperStatusUpdated", payload);
                await _hubContext.Clients.Group($"User_{authorId}")
                    .SendAsync("PaperStatusUpdated", payload);

                // 2. Gửi tới nhóm Admin
                await _hubContext.Clients.Group("Role_Admin")
                    .SendAsync("PaperStatusUpdated", payload);

                // 3. Gửi tới Group cụ thể của Paper (cho bất kỳ ai đang xem chi tiết bài này)
                await _hubContext.Clients.Group($"Paper_{paperId}")
                    .SendAsync("PaperStatusUpdated", payload);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SignalR] Error broadcasting PaperStatusUpdated: {ex.Message}");
            }
        }

        public async Task SendReviewRequestAssignedAsync(int reviewerId, int reviewRequestId, int paperId, string paperTitle, DateTime? deadline)
        {
            try
            {
                var payload = new
                {
                    reviewRequestId,
                    paperId,
                    paperTitle,
                    deadline
                };

                await _hubContext.Clients.User(reviewerId.ToString())
                    .SendAsync("ReviewRequestAssigned", payload);
                await _hubContext.Clients.Group($"User_{reviewerId}")
                    .SendAsync("ReviewRequestAssigned", payload);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SignalR] Error broadcasting ReviewRequestAssigned: {ex.Message}");
            }
        }

        public async Task SendGroupJoinRequestUpdatedAsync(int groupId, int requestId, string status, int applicantUserId, string applicantName)
        {
            try
            {
                var payload = new
                {
                    groupId,
                    requestId,
                    status,
                    applicantUserId,
                    applicantName
                };

                // Gửi tới workspace nhóm nghiên cứu
                await _hubContext.Clients.Group($"Group_{groupId}")
                    .SendAsync("GroupJoinRequestUpdated", payload);

                // Gửi tới sinh viên nộp đơn
                await _hubContext.Clients.User(applicantUserId.ToString())
                    .SendAsync("GroupJoinRequestUpdated", payload);
                await _hubContext.Clients.Group($"User_{applicantUserId}")
                    .SendAsync("GroupJoinRequestUpdated", payload);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SignalR] Error broadcasting GroupJoinRequestUpdated: {ex.Message}");
            }
        }

        public async Task SendForumCommentAddedAsync(int forumPostId, int forumCommentId, int userId, string authorName, string content, DateTime createdAt)
        {
            try
            {
                var payload = new
                {
                    forumPostId,
                    forumCommentId,
                    userId,
                    authorName,
                    content,
                    createdAt
                };

                await _hubContext.Clients.Group($"Post_{forumPostId}")
                    .SendAsync("ForumCommentAdded", payload);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SignalR] Error broadcasting ForumCommentAdded: {ex.Message}");
            }
        }

        public async Task SendMedalAwardedAsync(int userId, string medalId, string medalName, string? iconUrl, DateTime unlockedAt)
        {
            try
            {
                var payload = new
                {
                    medalId,
                    medalName,
                    iconUrl,
                    unlockedAt
                };

                await _hubContext.Clients.User(userId.ToString())
                    .SendAsync("MedalAwarded", payload);
                await _hubContext.Clients.Group($"User_{userId}")
                    .SendAsync("MedalAwarded", payload);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SignalR] Error broadcasting MedalAwarded: {ex.Message}");
            }
        }
    }
}
