using SanctionsAlertService.Domain.Events;

namespace SanctionsAlertService.Application.Events;

public interface IAlertEventPublisher
{
    Task PublishAsync(AlertEvent evt, CancellationToken cancellationToken = default);
}
