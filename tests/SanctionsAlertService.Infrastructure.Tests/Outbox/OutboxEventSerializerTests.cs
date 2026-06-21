using SanctionsAlertService.Domain.Enums;
using SanctionsAlertService.Domain.Events;
using SanctionsAlertService.Infrastructure.Outbox;

namespace SanctionsAlertService.Infrastructure.Tests.Outbox;

public sealed class OutboxEventSerializerTests
{
    private static readonly DateTimeOffset Now = new(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Serialize_UsesEventDiscriminator()
    {
        var serializer = new OutboxEventSerializer();
        var evt = new AlertEscalated(Guid.NewGuid(), Guid.NewGuid(), "tenant-1", "ESCALATED", "OPEN", Now);

        var (eventType, payload) = serializer.Serialize(evt);

        Assert.Equal("alert.escalated", eventType);
        Assert.False(string.IsNullOrWhiteSpace(payload));
    }

    [Fact]
    public void Deserialize_RoundTrips_AlertEscalated()
    {
        var serializer = new OutboxEventSerializer();
        var evt = new AlertEscalated(Guid.NewGuid(), Guid.NewGuid(), "tenant-1", "ESCALATED", "OPEN", Now);

        var (eventType, payload) = serializer.Serialize(evt);
        var roundTripped = serializer.Deserialize(eventType, payload);

        Assert.Equal(evt, roundTripped);
    }

    [Fact]
    public void Deserialize_RoundTrips_AlertDecided()
    {
        var serializer = new OutboxEventSerializer();
        var evt = new AlertDecided(Guid.NewGuid(), Guid.NewGuid(), "tenant-1", DecisionOutcome.CLEARED, Now);

        var (eventType, payload) = serializer.Serialize(evt);
        var roundTripped = serializer.Deserialize(eventType, payload);

        Assert.Equal(evt, roundTripped);
    }

    [Fact]
    public void Deserialize_UnknownEventType_Throws()
    {
        var serializer = new OutboxEventSerializer();

        Assert.Throws<NotSupportedException>(() => serializer.Deserialize("alert.unknown", "{}"));
    }
}
