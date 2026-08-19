using ARSPlatform.SERVICE.Interfaces;

namespace ARSPlatform.API.HostedServices
{
    public class SeminarAutomationHostedService : BackgroundService
    {
        private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SeminarAutomationHostedService> _logger;

        public SeminarAutomationHostedService(
            IServiceScopeFactory scopeFactory,
            ILogger<SeminarAutomationHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await RunAutomationAsync(stoppingToken);

            using var timer = new PeriodicTimer(CheckInterval);

            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    await RunAutomationAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
        }

        private async Task RunAutomationAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();

                var seminarService =
                    scope.ServiceProvider.GetRequiredService<ISeminarService>();

                await seminarService.UpdateLifecycleStatusesAsync(cancellationToken);
                await seminarService.SendDueEventRemindersAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Seminar background automation failed.");
            }
        }
    }
}