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

        /// <summary>
        /// Tham gia nhóm nhận cập nhật trạng thái của bài báo cụ thể.
        /// </summary>
        public async Task JoinPaperGroup(int paperId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Paper_{paperId}");
        }

        /// <summary>
        /// Rời khỏi nhóm nhận cập nhật trạng thái của bài báo.
        /// </summary>
        public async Task LeavePaperGroup(int paperId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Paper_{paperId}");
        }

        /// <summary>
        /// Tham gia luồng bình luận trực tiếp của bài viết diễn đàn cụ thể.
        /// </summary>
        public async Task JoinPostGroup(int postId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Post_{postId}");
        }

        /// <summary>
        /// Rời khỏi luồng bình luận trực tiếp của bài viết diễn đàn.
        /// </summary>
        public async Task LeavePostGroup(int postId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Post_{postId}");
        }

        /// <summary>
        /// Tham gia workspace trực tiếp của nhóm nghiên cứu.
        /// </summary>
        public async Task JoinGroupWorkspace(int groupId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Group_{groupId}");
        }

        /// <summary>
        /// Rời khỏi workspace trực tiếp của nhóm nghiên cứu.
        /// </summary>
        public async Task LeaveGroupWorkspace(int groupId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Group_{groupId}");
        }
    }
}
