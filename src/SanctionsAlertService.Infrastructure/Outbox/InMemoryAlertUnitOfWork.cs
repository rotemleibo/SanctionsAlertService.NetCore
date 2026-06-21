using SanctionsAlertService.Application.Outbox;
using SanctionsAlertService.Domain.Entities;
using SanctionsAlertService.Domain.Events;
using SanctionsAlertService.Infrastructure.Persistence;

namespace SanctionsAlertService.Infrastructure.Outbox;

public sealed class InMemoryAlertUnitOfWork(
    InMemoryDatabase database,
    IOutboxEventSerializer serializer,
    TimeProvider timeProvider) : IAlertUnitOfWork
{
    public Task<Alert> SaveAsync(
        string tenantId,
        Alert alert,
        IReadOnlyCollection<AlertEvent> events,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var occurredAtUtc = timeProvider.GetUtcNow();
        var messages = new List<OutboxMessage>(events.Count);

        foreach (var evt in events)
        {
            var (eventType, payload) = serializer.Serialize(evt);
            messages.Add(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                TenantId = evt.TenantId,
                EventId = evt.EventId,
                EventType = eventType,
                Payload = payload,
                OccurredAtUtc = occurredAtUtc
            });
        }

        var saved = database.SaveAlert(tenantId, alert, messages);
        return Task.FromResult(saved);
    }
}
