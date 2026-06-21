using SanctionsAlertService.Application.Outbox;
using SanctionsAlertService.Domain.Entities;
using SanctionsAlertService.Domain.Exceptions;
using SanctionsAlertService.Domain.ValueObjects;

namespace SanctionsAlertService.Infrastructure.Persistence;

/// <summary>
/// Single in-memory source of truth for alerts and outbox messages. All writes are
/// serialized through one lock so that an alert and its outbox messages commit atomically,
/// which is the guarantee the transactional outbox relies on.
/// </summary>
public sealed class InMemoryDatabase
{
    private readonly Lock _gate = new();
    private readonly Dictionary<string, Dictionary<Guid, Alert>> _alerts = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Dictionary<string, Guid>> _transactionIndex = new(StringComparer.Ordinal);
    private readonly Dictionary<Guid, OutboxMessage> _outbox = [];

    public Alert SaveAlert(string tenantId, Alert alert, IReadOnlyCollection<OutboxMessage> outboxMessages)
    {
        ValidateTenant(tenantId);

        if (!string.Equals(tenantId, alert.TenantId, StringComparison.Ordinal))
        {
            throw new ArgumentException("Tenant mismatch between scope and alert payload.", nameof(tenantId));
        }

        var normalizedTransactionId = NormalizeTransactionId(alert.TransactionId);

        lock (_gate)
        {
            if (!_alerts.TryGetValue(tenantId, out var tenantBucket))
            {
                tenantBucket = [];
                _alerts[tenantId] = tenantBucket;
            }

            if (!_transactionIndex.TryGetValue(tenantId, out var tenantTransactionIndex))
            {
                tenantTransactionIndex = new Dictionary<string, Guid>(StringComparer.Ordinal);
                _transactionIndex[tenantId] = tenantTransactionIndex;
            }

            if (tenantBucket.TryGetValue(alert.Id, out var existing))
            {
                if (existing.Version + 1 != alert.Version)
                {
                    throw new OptimisticLockException(alert.Id.ToString());
                }

                if (!string.Equals(existing.TransactionId, normalizedTransactionId, StringComparison.Ordinal))
                {
                    throw new ArgumentException("Transaction id cannot be changed for an existing alert.", nameof(alert));
                }
            }
            else
            {
                if (alert.Version != 0)
                {
                    throw new OptimisticLockException(alert.Id.ToString());
                }

                if (tenantTransactionIndex.ContainsKey(normalizedTransactionId))
                {
                    throw new TransactionAlreadyExistsException(normalizedTransactionId);
                }
            }

            tenantTransactionIndex[normalizedTransactionId] = alert.Id;
            tenantBucket[alert.Id] = alert;

            foreach (var message in outboxMessages)
            {
                _outbox[message.Id] = message;
            }

            return alert;
        }
    }

    public Alert? FindAlertById(string tenantId, Guid alertId)
    {
        ValidateTenant(tenantId);

        lock (_gate)
        {
            if (!_alerts.TryGetValue(tenantId, out var tenantBucket))
            {
                return null;
            }

            tenantBucket.TryGetValue(alertId, out var alert);
            return alert;
        }
    }

    public Alert? FindAlertByTransactionId(string tenantId, string transactionId)
    {
        ValidateTenant(tenantId);

        var normalizedTransactionId = NormalizeTransactionId(transactionId);

        lock (_gate)
        {
            if (!_alerts.TryGetValue(tenantId, out var tenantBucket) ||
                !_transactionIndex.TryGetValue(tenantId, out var tenantTransactionIndex) ||
                !tenantTransactionIndex.TryGetValue(normalizedTransactionId, out var alertId))
            {
                return null;
            }

            tenantBucket.TryGetValue(alertId, out var alert);
            return alert;
        }
    }

    public IReadOnlyCollection<Alert> FindAlerts(string tenantId, AlertFilter filter)
    {
        ValidateTenant(tenantId);

        lock (_gate)
        {
            if (!_alerts.TryGetValue(tenantId, out var tenantBucket))
            {
                return [];
            }

            IEnumerable<Alert> query = tenantBucket.Values;

            if (filter.Status.HasValue)
            {
                query = query.Where(a => a.Status == filter.Status.Value);
            }

            if (filter.MinMatchScore.HasValue)
            {
                query = query.Where(a => a.MatchScore >= filter.MinMatchScore.Value);
            }

            return query
                .OrderByDescending(a => a.CreatedAt)
                .ToArray();
        }
    }

    public void AddOutboxMessages(IReadOnlyCollection<OutboxMessage> messages)
    {
        lock (_gate)
        {
            foreach (var message in messages)
            {
                _outbox[message.Id] = message;
            }
        }
    }

    public IReadOnlyCollection<OutboxMessage> ClaimPending(int batchSize, DateTimeOffset nowUtc, DateTimeOffset leasedUntilUtc)
    {
        lock (_gate)
        {
            var claimed = _outbox.Values
                .Where(m => m.Status == OutboxMessageStatus.Pending
                    && (m.NextAttemptAtUtc is null || m.NextAttemptAtUtc <= nowUtc))
                .OrderBy(m => m.OccurredAtUtc)
                .Take(batchSize)
                .ToArray();

            foreach (var message in claimed)
            {
                message.Lease(leasedUntilUtc);
            }

            return claimed;
        }
    }

    public void MarkProcessed(Guid messageId, DateTimeOffset processedAtUtc)
    {
        lock (_gate)
        {
            if (_outbox.TryGetValue(messageId, out var message))
            {
                message.MarkProcessed(processedAtUtc);
            }
        }
    }

    public void MarkFailed(Guid messageId, string error, DateTimeOffset nextAttemptAtUtc)
    {
        lock (_gate)
        {
            if (_outbox.TryGetValue(messageId, out var message))
            {
                message.RecordFailure(error);
                message.Reschedule(nextAttemptAtUtc);
            }
        }
    }

    public void MarkDeadLetter(Guid messageId, string error)
    {
        lock (_gate)
        {
            if (_outbox.TryGetValue(messageId, out var message))
            {
                message.RecordFailure(error);
                message.MarkDeadLetter();
            }
        }
    }

    public IReadOnlyCollection<OutboxMessage> GetOutboxMessages()
    {
        lock (_gate)
        {
            return _outbox.Values.ToArray();
        }
    }

    private static void ValidateTenant(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException("Tenant id is required.", nameof(tenantId));
        }
    }

    private static string NormalizeTransactionId(string transactionId)
    {
        if (string.IsNullOrWhiteSpace(transactionId))
        {
            throw new ArgumentException("Transaction id is required.", nameof(transactionId));
        }

        return transactionId.Trim();
    }
}
