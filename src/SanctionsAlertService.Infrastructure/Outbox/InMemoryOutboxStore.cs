using SanctionsAlertService.Application.Outbox;
using SanctionsAlertService.Infrastructure.Persistence;

namespace SanctionsAlertService.Infrastructure.Outbox;

public sealed class InMemoryOutboxStore(InMemoryDatabase database) : IOutboxStore
{
    public Task AddBatchAsync(IReadOnlyCollection<OutboxMessage> messages, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        database.AddOutboxMessages(messages);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<OutboxMessage>> ClaimPendingAsync(
        int batchSize,
        DateTimeOffset nowUtc,
        DateTimeOffset leasedUntilUtc,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(database.ClaimPending(batchSize, nowUtc, leasedUntilUtc));
    }

    public Task MarkProcessedAsync(Guid messageId, DateTimeOffset processedAtUtc, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        database.MarkProcessed(messageId, processedAtUtc);
        return Task.CompletedTask;
    }

    public Task MarkFailedAsync(Guid messageId, string error, DateTimeOffset nextAttemptAtUtc, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        database.MarkFailed(messageId, error, nextAttemptAtUtc);
        return Task.CompletedTask;
    }

    public Task MarkDeadLetterAsync(Guid messageId, string error, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        database.MarkDeadLetter(messageId, error);
        return Task.CompletedTask;
    }
}
