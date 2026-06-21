using SanctionsAlertService.Domain.Entities;
using SanctionsAlertService.Domain.Events;

namespace SanctionsAlertService.Application.Outbox;

public interface IAlertUnitOfWork
{
    Task<Alert> SaveAsync(
        string tenantId,
        Alert alert,
        IReadOnlyCollection<AlertEvent> events,
        CancellationToken cancellationToken = default);
}
