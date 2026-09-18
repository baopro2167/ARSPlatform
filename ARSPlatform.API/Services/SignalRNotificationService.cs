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

                // Gửi tới tác giả bài báo
                await _hubContext.Clients.User(authorId.ToString())
                    .SendAsync("PaperStatusUpdated", payload);
                await _hubContext.Clients.Group($"User_{authorId}")
                    .SendAsync("PaperStatusUpdated", payload);

                // Đồng thời gửi tới nhóm Admin
                await _hubContext.Clients.Group("Role_Admin")
                    .SendAsync("PaperStatusUpdated", payload);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SignalR] Error broadcasting PaperStatusUpdated: {ex.Message}");
            }
        }
    }
}
