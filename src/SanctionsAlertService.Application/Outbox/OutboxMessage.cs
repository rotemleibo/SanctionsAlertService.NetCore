namespace SanctionsAlertService.Application.Outbox;

public sealed class OutboxMessage
{
    public required Guid Id { get; init; }

    public required string TenantId { get; init; }

    public required Guid EventId { get; init; }

    public required string EventType { get; init; }

    public required string Payload { get; init; }

    public required DateTimeOffset OccurredAtUtc { get; init; }

    public OutboxMessageStatus Status { get; private set; } = OutboxMessageStatus.Pending;

    public int Attempts { get; private set; }

    public DateTimeOffset? NextAttemptAtUtc { get; private set; }

    public DateTimeOffset? LeasedUntilUtc { get; private set; }

    public string? LastError { get; private set; }

    public DateTimeOffset? ProcessedAtUtc { get; private set; }

    public void Lease(DateTimeOffset leasedUntilUtc)
    {
        Status = OutboxMessageStatus.Processing;
        LeasedUntilUtc = leasedUntilUtc;
    }

    public void MarkProcessed(DateTimeOffset processedAtUtc)
    {
        Status = OutboxMessageStatus.Processed;
        ProcessedAtUtc = processedAtUtc;
        NextAttemptAtUtc = null;
        LeasedUntilUtc = null;
        LastError = null;
    }

    public void RecordFailure(string error)
    {
        Attempts++;
        LastError = error;
    }

    public void Reschedule(DateTimeOffset nextAttemptAtUtc)
    {
        Status = OutboxMessageStatus.Pending;
        NextAttemptAtUtc = nextAttemptAtUtc;
        LeasedUntilUtc = null;
    }

    public void MarkDeadLetter()
    {
        Status = OutboxMessageStatus.DeadLetter;
        NextAttemptAtUtc = null;
        LeasedUntilUtc = null;
    }
}
