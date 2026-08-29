using ARSPlatform.MODEL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ARSPlatform.API.HostedServices
{
    public class OtpCleanupHostedService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OtpCleanupHostedService> _logger;

        public OtpCleanupHostedService(
            IServiceProvider serviceProvider,
            ILogger<OtpCleanupHostedService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("OTP Cleanup Service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupExpiredOtpsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during OTP cleanup.");
                }

                // Chạy mỗi 1 giờ
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        private async Task CleanupExpiredOtpsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var now = DateTime.UtcNow;

            // Tìm tất cả user có OTP đã hết hạn hoặc đã được sử dụng
            var expiredOtps = await context.Users
                .Where(u => (u.ExpiresOtpAt != null && u.ExpiresOtpAt < now) || u.IsOtpUsed == true)
                .ToListAsync();

            if (expiredOtps.Any())
            {
                foreach (var user in expiredOtps)
                {
                    user.OtpCode = null;
                    user.ExpiresOtpAt = null;
                    user.IsOtpUsed = null;
                }

                await context.SaveChangesAsync();
                _logger.LogInformation($"Cleaned up {expiredOtps.Count} expired/used OTP records.");
            }
            else
            {
                _logger.LogInformation("No expired OTP records found.");
            }
        }
    }
}
