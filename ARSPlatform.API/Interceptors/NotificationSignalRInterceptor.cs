using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using ARSPlatform.API.Hubs;
using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.API.Interceptors
{
    /// <summary>
    /// SaveChangesInterceptor tự động bắt tất cả các thực thể Notification được thêm mới hoặc cập nhật IsRead
    /// trên toàn bộ 16+ services trong hệ thống và tự động phát sóng qua SignalR tới người dùng.
    /// </summary>
    public class NotificationSignalRInterceptor : SaveChangesInterceptor
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly List<Notification> _addedNotifications = new();
        private readonly HashSet<int> _affectedUserIds = new();

        public NotificationSignalRInterceptor(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            CaptureChanges(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            CaptureChanges(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private void CaptureChanges(DbContext? context)
        {
            _addedNotifications.Clear();
            _affectedUserIds.Clear();

            if (context == null) return;

            var entries = context.ChangeTracker.Entries<Notification>().ToList();
            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added)
                {
                    _addedNotifications.Add(entry.Entity);
                    if (entry.Entity.UserId.HasValue && entry.Entity.UserId.Value > 0)
                    {
                        _affectedUserIds.Add(entry.Entity.UserId.Value);
                    }
                }
                else if (entry.State == EntityState.Modified)
                {
                    var isReadModified = entry.Property(p => p.IsRead).IsModified;
                    if (isReadModified && entry.Entity.UserId.HasValue && entry.Entity.UserId.Value > 0)
                    {
                        _affectedUserIds.Add(entry.Entity.UserId.Value);
                    }
                }
                else if (entry.State == EntityState.Deleted)
                {
                    if (entry.Entity.UserId.HasValue && entry.Entity.UserId.Value > 0)
                    {
                        _affectedUserIds.Add(entry.Entity.UserId.Value);
                    }
                }
            }
        }

        public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
        {
            DispatchNotifications();
            return base.SavedChanges(eventData, result);
        }

        public override ValueTask<int> SavedChangesAsync(
            SaveChangesCompletedEventData eventData,
            int result,
            CancellationToken cancellationToken = default)
        {
            DispatchNotifications();
            return base.SavedChangesAsync(eventData, result, cancellationToken);
        }

        private void DispatchNotifications()
        {
            if (!_addedNotifications.Any() && !_affectedUserIds.Any())
            {
                return;
            }

            var notificationsToSend = _addedNotifications.Select(n => new NotificationResponse
            {
                NotificationId = n.NotificationId,
                UserId = n.UserId,
                Message = n.Message,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            }).ToList();

            var userIdsToUpdate = _affectedUserIds.ToList();

            _addedNotifications.Clear();
            _affectedUserIds.Clear();

            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<NotificationHub>>();
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    // 1. Gửi sự kiện ReceiveNotification cho các thông báo mới
                    foreach (var notif in notificationsToSend)
                    {
                        if (notif.UserId.HasValue && notif.UserId.Value > 0)
                        {
                            var userIdStr = notif.UserId.Value.ToString();
                            await hubContext.Clients.User(userIdStr)
                                .SendAsync("ReceiveNotification", notif);
                            await hubContext.Clients.Group($"User_{notif.UserId.Value}")
                                .SendAsync("ReceiveNotification", notif);
                        }
                    }

                    // 2. Tính toán và gửi cập nhật số lượng thông báo chưa đọc (UpdateUnreadCount)
                    foreach (var userId in userIdsToUpdate)
                    {
                        if (userId > 0)
                        {
                            var unreadCount = await dbContext.Notifications
                                .CountAsync(n => n.UserId == userId && (n.IsRead == false || n.IsRead == null));

                            var userIdStr = userId.ToString();
                            await hubContext.Clients.User(userIdStr)
                                .SendAsync("UpdateUnreadCount", unreadCount);
                            await hubContext.Clients.Group($"User_{userId}")
                                .SendAsync("UpdateUnreadCount", unreadCount);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[NotificationSignalRInterceptor Error] {ex.Message}");
                }
            });
        }
    }
}
