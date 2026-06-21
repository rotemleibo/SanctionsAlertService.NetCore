using SanctionsAlertService.Application.Outbox;
using SanctionsAlertService.Domain.Entities;
using SanctionsAlertService.Infrastructure.Outbox;
using SanctionsAlertService.Infrastructure.Persistence;
using SanctionsAlertService.Infrastructure.Tests.TestDoubles;

namespace SanctionsAlertService.Infrastructure.Tests.Outbox;

public sealed class InMemoryOutboxStoreTests
{
    private static readonly DateTimeOffset Now = new(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ClaimPending_ReturnsPendingMessage_AndLeasesIt()
    {
        var database = new InMemoryDatabase();
        var store = new InMemoryOutboxStore(database);
        var message = CreateMessage();
        await store.AddBatchAsync([message]);

        var claimed = await store.ClaimPendingAsync(10, Now, Now.AddSeconds(30));

        var single = Assert.Single(claimed);
        Assert.Equal(message.Id, single.Id);
        Assert.Equal(OutboxMessageStatus.Processing, single.Status);
    }

    [Fact]
    public async Task ClaimPending_DoesNotReturnMessage_ScheduledForFuture()
    {
        var database = new InMemoryDatabase();
        var store = new InMemoryOutboxStore(database);
        var message = CreateMessage();
        await store.AddBatchAsync([message]);
        await store.MarkFailedAsync(message.Id, "boom", Now.AddMinutes(5));

        var claimed = await store.ClaimPendingAsync(10, Now, Now.AddSeconds(30));

        Assert.Empty(claimed);
    }

    [Fact]
    public async Task MarkProcessed_MovesMessageToProcessed()
    {
        var database = new InMemoryDatabase();
        var store = new InMemoryOutboxStore(database);
        var message = CreateMessage();
        await store.AddBatchAsync([message]);

        await store.MarkProcessedAsync(message.Id, Now);

        var stored = Assert.Single(database.GetOutboxMessages());
        Assert.Equal(OutboxMessageStatus.Processed, stored.Status);
        Assert.Equal(Now, stored.ProcessedAtUtc);
    }

    [Fact]
    public async Task MarkFailed_IncrementsAttempts_AndReschedules()
    {
        var database = new InMemoryDatabase();
        var store = new InMemoryOutboxStore(database);
        var message = CreateMessage();
        await store.AddBatchAsync([message]);

        await store.MarkFailedAsync(message.Id, "boom", Now.AddMinutes(5));

        var stored = Assert.Single(database.GetOutboxMessages());
        Assert.Equal(OutboxMessageStatus.Pending, stored.Status);
        Assert.Equal(1, stored.Attempts);
        Assert.Equal(Now.AddMinutes(5), stored.NextAttemptAtUtc);
        Assert.Equal("boom", stored.LastError);
    }

    [Fact]
    public async Task MarkDeadLetter_MovesMessageToDeadLetter()
    {
        var database = new InMemoryDatabase();
        var store = new InMemoryOutboxStore(database);
        var message = CreateMessage();
        await store.AddBatchAsync([message]);

        await store.MarkDeadLetterAsync(message.Id, "poison");

        var stored = Assert.Single(database.GetOutboxMessages());
        Assert.Equal(OutboxMessageStatus.DeadLetter, stored.Status);
        Assert.Equal("poison", stored.LastError);
    }

    private static OutboxMessage CreateMessage() => new()
    {
        Id = Guid.NewGuid(),
        TenantId = "tenant-1",
        EventId = Guid.NewGuid(),
        EventType = "alert.escalated",
        Payload = "{}",
        OccurredAtUtc = Now
    };
}
