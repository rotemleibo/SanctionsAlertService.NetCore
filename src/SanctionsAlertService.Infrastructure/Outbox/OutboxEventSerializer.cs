using System.Text.Json;
using SanctionsAlertService.Application.Outbox;
using SanctionsAlertService.Domain.Events;

namespace SanctionsAlertService.Infrastructure.Outbox;

public sealed class OutboxEventSerializer : IOutboxEventSerializer
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private static readonly IReadOnlyDictionary<string, Type> EventTypes = new Dictionary<string, Type>(StringComparer.Ordinal)
    {
        ["alert.escalated"] = typeof(AlertEscalated),
        ["alert.decided"] = typeof(AlertDecided)
    };

    public (string EventType, string Payload) Serialize(AlertEvent evt)
    {
        ArgumentNullException.ThrowIfNull(evt);

        if (!EventTypes.ContainsKey(evt.Event))
        {
            throw new NotSupportedException($"Unknown outbox event type '{evt.Event}'.");
        }

        var payload = JsonSerializer.Serialize(evt, evt.GetType(), SerializerOptions);
        return (evt.Event, payload);
    }

    public AlertEvent Deserialize(string eventType, string payload)
    {
        if (!EventTypes.TryGetValue(eventType, out var clrType))
        {
            throw new NotSupportedException($"Unknown outbox event type '{eventType}'.");
        }

        if (JsonSerializer.Deserialize(payload, clrType, SerializerOptions) is not AlertEvent evt)
        {
            throw new InvalidOperationException($"Failed to deserialize outbox payload for event type '{eventType}'.");
        }

        return evt;
    }
}
