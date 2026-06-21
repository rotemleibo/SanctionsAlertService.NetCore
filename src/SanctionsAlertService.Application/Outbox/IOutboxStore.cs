namespace SanctionsAlertService.Application.Outbox;

public interface IOutboxStore
{
    Task AddBatchAsync(
        IReadOnlyCollection<OutboxMessage> messages,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<OutboxMessage>> ClaimPendingAsync(
        int batchSize,
        DateTimeOffset nowUtc,
        DateTimeOffset leasedUntilUtc,
        CancellationToken cancellationToken = default);

    Task MarkProcessedAsync(
        Guid messageId,
        DateTimeOffset processedAtUtc,
        CancellationToken cancellationToken = default);

    Task MarkFailedAsync(
        Guid messageId,
        string error,
        DateTimeOffset nextAttemptAtUtc,
        CancellationToken cancellationToken = default);

    Task MarkDeadLetterAsync(
        Guid messageId,
        string error,
        CancellationToken cancellationToken = default);
}
