using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SanctionsAlertService.Application.Outbox;

namespace SanctionsAlertService.Infrastructure.Outbox;

public sealed class OutboxDispatcherService(
    IServiceScopeFactory scopeFactory,
    OutboxOptions options,
    TimeProvider timeProvider,
    ILogger<OutboxDispatcherService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Outbox dispatcher started; polling every {Interval}.", options.PollingInterval);

        using var timer = new PeriodicTimer(options.PollingInterval, timeProvider);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
                await DispatchOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled error while dispatching outbox messages.");
            }
        }

        logger.LogInformation("Outbox dispatcher stopped.");
    }

    private async Task DispatchOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<OutboxProcessor>();
        await processor.ProcessBatchAsync(cancellationToken);
    }
}
