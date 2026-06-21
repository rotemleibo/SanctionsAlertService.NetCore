using Microsoft.Extensions.Logging.Abstractions;
using SanctionsAlertService.Application.Outbox;
using SanctionsAlertService.Domain.Entities;
using SanctionsAlertService.Domain.Events;
using SanctionsAlertService.Infrastructure.Outbox;
using SanctionsAlertService.Infrastructure.Persistence;
using SanctionsAlertService.Infrastructure.Tests.TestDoubles;

namespace SanctionsAlertService.Infrastructure.Tests.Outbox;

public sealed class OutboxProcessorTests
{
    private static readonly DateTimeOffset Now = new(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ProcessBatch_PublishesPendingMessage_AndMarksProcessed()
    {
        var harness = new Harness();
        await harness.EnqueueEscalatedAsync();

        var processed = await harness.Processor.ProcessBatchAsync();

        Assert.Equal(1, processed);
        Assert.Single(harness.Publisher.Published);
        var stored = Assert.Single(harness.Database.GetOutboxMessages());
        Assert.Equal(OutboxMessageStatus.Processed, stored.Status);
    }

    [Fact]
    public async Task ProcessBatch_WhenPublishFails_ReschedulesWithBackoff()
    {
        var harness = new Harness(shouldThrow: _ => true);
        await harness.EnqueueEscalatedAsync();

        var processed = await harness.Processor.ProcessBatchAsync();

        Assert.Equal(0, processed);
        var stored = Assert.Single(harness.Database.GetOutboxMessages());
        Assert.Equal(OutboxMessageStatus.Pending, stored.Status);
        Assert.Equal(1, stored.Attempts);
        Assert.Equal(Now + harness.Options.BaseBackoff, stored.NextAttemptAtUtc);
    }

    [Fact]
    public async Task ProcessBatch_WhenAttemptsReachMax_MovesToDeadLetter()
    {
        var harness = new Harness(shouldThrow: _ => true);
        harness.Options.MaxAttempts = 1;
        await harness.EnqueueEscalatedAsync();

        var processed = await harness.Processor.ProcessBatchAsync();

        Assert.Equal(0, processed);
        var stored = Assert.Single(harness.Database.GetOutboxMessages());
        Assert.Equal(OutboxMessageStatus.DeadLetter, stored.Status);
        Assert.Equal(1, stored.Attempts);
    }

    [Fact]
    public async Task ProcessBatch_WhenPayloadIsPoison_MovesToDeadLetter()
    {
        var harness = new Harness();
        await harness.Store.AddBatchAsync([new OutboxMessage
        {
            Id = Guid.NewGuid(),
            TenantId = "tenant-1",
            EventId = Guid.NewGuid(),
            EventType = "alert.unknown",
            Payload = "{}",
            OccurredAtUtc = Now
        }]);

        var processed = await harness.Processor.ProcessBatchAsync();

        Assert.Equal(0, processed);
        Assert.Empty(harness.Publisher.Published);
        var stored = Assert.Single(harness.Database.GetOutboxMessages());
        Assert.Equal(OutboxMessageStatus.DeadLetter, stored.Status);
    }

    [Fact]
    public async Task ProcessBatch_BackoffGrowsExponentially_AcrossRetries()
    {
        var harness = new Harness(shouldThrow: _ => true);
        await harness.EnqueueEscalatedAsync();

        await harness.Processor.ProcessBatchAsync();
        var afterFirst = Assert.Single(harness.Database.GetOutboxMessages());
        Assert.Equal(Now + harness.Options.BaseBackoff, afterFirst.NextAttemptAtUtc);

        harness.TimeProvider.Advance(harness.Options.BaseBackoff);
        await harness.Processor.ProcessBatchAsync();
        var afterSecond = Assert.Single(harness.Database.GetOutboxMessages());
        Assert.Equal(2, afterSecond.Attempts);
        var expectedSecond = Now + harness.Options.BaseBackoff + (harness.Options.BaseBackoff * 2);
        Assert.Equal(expectedSecond, afterSecond.NextAttemptAtUtc);
    }

    private sealed class Harness
    {
        public Harness(Func<AlertEvent, bool>? shouldThrow = null)
        {
            Database = new InMemoryDatabase();
            Store = new InMemoryOutboxStore(Database);
            Serializer = new OutboxEventSerializer();
            Publisher = new FakeAlertEventPublisher(shouldThrow);
            TimeProvider = new TestTimeProvider(Now);
            Options = new OutboxOptions();
            Processor = new OutboxProcessor(
                Store,
                Serializer,
                Publisher,
                Options,
                TimeProvider,
                NullLogger<OutboxProcessor>.Instance);
        }

        public InMemoryDatabase Database { get; }

        public InMemoryOutboxStore Store { get; }

        public OutboxEventSerializer Serializer { get; }

        public FakeAlertEventPublisher Publisher { get; }

        public TestTimeProvider TimeProvider { get; }

        public OutboxOptions Options { get; }

        public OutboxProcessor Processor { get; }

        public async Task EnqueueEscalatedAsync()
        {
            var alert = Alert.Create("tenant-1", "txn-1", "Acme", 90, null, Now);
            var unitOfWork = new InMemoryAlertUnitOfWork(Database, Serializer, TimeProvider);
            var evt = new AlertEscalated(Guid.NewGuid(), alert.Id, alert.TenantId, "ESCALATED", "OPEN", Now);
            await unitOfWork.SaveAsync("tenant-1", alert, [evt]);
        }
    }
}
