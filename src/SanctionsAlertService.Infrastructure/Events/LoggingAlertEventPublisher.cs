using System.Text.Json;
using Microsoft.Extensions.Logging;
using SanctionsAlertService.Application.Events;
using SanctionsAlertService.Domain.Events;

namespace SanctionsAlertService.Infrastructure.Events;

public sealed class LoggingAlertEventPublisher(ILogger<LoggingAlertEventPublisher> logger) : IAlertEventPublisher
{
    public Task PublishAsync(AlertEvent evt, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var payload = JsonSerializer.Serialize(evt);
        logger.LogInformation("Published alert event {EventType}: {Payload}", evt.Event, payload);

        return Task.CompletedTask;
    }
}
