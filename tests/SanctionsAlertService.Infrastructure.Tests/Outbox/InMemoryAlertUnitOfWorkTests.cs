using SanctionsAlertService.Application.Outbox;
using SanctionsAlertService.Domain.Entities;
using SanctionsAlertService.Domain.Events;
using SanctionsAlertService.Infrastructure.Outbox;
using SanctionsAlertService.Infrastructure.Persistence;
using SanctionsAlertService.Infrastructure.Tests.TestDoubles;

namespace SanctionsAlertService.Infrastructure.Tests.Outbox;

public sealed class InMemoryAlertUnitOfWorkTests
{
    private static readonly DateTimeOffset Now = new(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SaveAsync_PersistsAlert_AndEnqueuesOneOutboxMessage()
    {
        var database = new InMemoryDatabase();
        var timeProvider = new TestTimeProvider(Now);
        var unitOfWork = new InMemoryAlertUnitOfWork(database, new OutboxEventSerializer(), timeProvider);
        var alert = Alert.Create("tenant-1", "txn-1", "Acme", 90, null, Now);
        var evt = new AlertEscalated(Guid.NewGuid(), alert.Id, alert.TenantId, "ESCALATED", "OPEN", Now);

        var saved = await unitOfWork.SaveAsync("tenant-1", alert, [evt]);

        Assert.Equal(alert.Id, saved.Id);
        Assert.NotNull(database.FindAlertById("tenant-1", alert.Id));

        var message = Assert.Single(database.GetOutboxMessages());
        Assert.Equal("alert.escalated", message.EventType);
        Assert.Equal(evt.EventId, message.EventId);
        Assert.Equal(OutboxMessageStatus.Pending, message.Status);
        Assert.Equal(Now, message.OccurredAtUtc);
    }

    [Fact]
    public async Task SaveAsync_WithNoEvents_PersistsAlert_WithoutOutboxMessages()
    {
        var database = new InMemoryDatabase();
        var unitOfWork = new InMemoryAlertUnitOfWork(database, new OutboxEventSerializer(), new TestTimeProvider(Now));
        var alert = Alert.Create("tenant-1", "txn-1", "Acme", 90, null, Now);

        await unitOfWork.SaveAsync("tenant-1", alert, []);

        Assert.NotNull(database.FindAlertById("tenant-1", alert.Id));
        Assert.Empty(database.GetOutboxMessages());
    }
}
