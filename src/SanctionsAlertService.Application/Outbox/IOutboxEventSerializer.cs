using SanctionsAlertService.Domain.Events;

namespace SanctionsAlertService.Application.Outbox;

public interface IOutboxEventSerializer
{
    (string EventType, string Payload) Serialize(AlertEvent evt);

    AlertEvent Deserialize(string eventType, string payload);
}
