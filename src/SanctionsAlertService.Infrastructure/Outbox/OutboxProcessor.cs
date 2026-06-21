using Microsoft.Extensions.Logging;
using SanctionsAlertService.Application.Events;
using SanctionsAlertService.Application.Outbox;
using SanctionsAlertService.Domain.Events;

namespace SanctionsAlertService.Infrastructure.Outbox;

public sealed class OutboxProcessor(
    IOutboxStore store,
    IOutboxEventSerializer serializer,
    IAlertEventPublisher publisher,
    OutboxOptions options,
    TimeProvider timeProvider,
    ILogger<OutboxProcessor> logger)
{
    public async Task<int> ProcessBatchAsync(CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var leasedUntil = now + options.LeaseDuration;
        var messages = await store.ClaimPendingAsync(options.BatchSize, now, leasedUntil, cancellationToken);

        var processed = 0;
        foreach (var message in messages)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (await TryDispatchAsync(message, cancellationToken))
            {
                processed++;
            }
        }

        return processed;
    }

    private async Task<bool> TryDispatchAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        AlertEvent evt;
        try
        {
            evt = serializer.Deserialize(message.EventType, message.Payload);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Dead-lettering outbox message {MessageId}: payload could not be deserialized.", message.Id);
            await store.MarkDeadLetterAsync(message.Id, ex.Message, cancellationToken);
            return false;
        }

        try
        {
            await publisher.PublishAsync(evt, cancellationToken);
            await store.MarkProcessedAsync(message.Id, timeProvider.GetUtcNow(), cancellationToken);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var attemptNumber = message.Attempts + 1;
            if (attemptNumber >= options.MaxAttempts)
            {
                logger.LogError(ex, "Dead-lettering outbox message {MessageId} after {Attempts} attempt(s).", message.Id, attemptNumber);
                await store.MarkDeadLetterAsync(message.Id, ex.Message, cancellationToken);
            }
            else
            {
                var nextAttemptAtUtc = timeProvider.GetUtcNow() + ComputeBackoff(attemptNumber);
                logger.LogWarning(
                    ex,
                    "Outbox message {MessageId} failed on attempt {Attempts}; next retry at {NextAttempt}.",
                    message.Id,
                    attemptNumber,
                    nextAttemptAtUtc);
                await store.MarkFailedAsync(message.Id, ex.Message, nextAttemptAtUtc, cancellationToken);
            }

            return false;
        }
    }

    private TimeSpan ComputeBackoff(int attemptNumber)
    {
        var exponent = Math.Min(attemptNumber - 1, 30);
        var backoffTicks = options.BaseBackoff.Ticks * Math.Pow(2, exponent);

        if (double.IsInfinity(backoffTicks) || backoffTicks >= options.MaxBackoff.Ticks)
        {
            return options.MaxBackoff;
        }

        return TimeSpan.FromTicks((long)backoffTicks);
    }
}
