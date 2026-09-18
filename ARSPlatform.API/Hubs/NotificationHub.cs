using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ARSPlatform.API.Hubs
{
    /// <summary>
    /// SignalR Hub chịu trách nhiệm truyền tải thông báo và cập nhật trạng thái Real-time cho người dùng.
    /// </summary>
    [Authorize]
    public class NotificationHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier
                ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                // Thêm connection vào Group riêng theo UserId: "User_{userId}"
                await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
            }

            var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value;
            if (!string.IsNullOrEmpty(role))
            {
                // Thêm connection vào Group theo Role (ví dụ: Role_Admin)
                await Groups.AddToGroupAsync(Context.ConnectionId, $"Role_{role}");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier
                ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userId}");
            }

            var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value;
            if (!string.IsNullOrEmpty(role))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Role_{role}");
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
