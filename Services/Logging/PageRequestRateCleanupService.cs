
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Host.Services.Logging;

/**
Runs cleanup approximately once per minute.

Removes IP entries that have no requests within the last five minutes.

Logs cleanup errors instead of allowing an ordinary cleanup error to terminate the service.

Does not interfere with your existing IP classification or evaluation queue.
*/
public class PageRequestRateCleanupService : BackgroundService
{
    private readonly ILogger<PageRequestRateCleanupService> _logger;

    public PageRequestRateCleanupService(
        ILogger<PageRequestRateCleanupService> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(
            TimeSpan.FromMinutes(1));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                PageRequestRateStore.CleanupInactiveIps(
                    TimeSpan.FromMinutes(5));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error cleaning up page request rate entries.");
            }
        }
    }
}