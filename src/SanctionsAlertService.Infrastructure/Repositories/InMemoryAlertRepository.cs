using System.Collections.Concurrent;
using SanctionsAlertService.Application.Repositories;
using SanctionsAlertService.Domain.Entities;
using SanctionsAlertService.Domain.Exceptions;
using SanctionsAlertService.Domain.ValueObjects;

namespace SanctionsAlertService.Infrastructure.Repositories;

public sealed class InMemoryAlertRepository : IAlertRepository
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, Alert>> _store = new(StringComparer.Ordinal);

    public Task<Alert> SaveAsync(string tenantId, Alert alert, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateTenant(tenantId);

        if (!string.Equals(tenantId, alert.TenantId, StringComparison.Ordinal))
        {
            throw new ArgumentException("Tenant mismatch between scope and alert payload.", nameof(tenantId));
        }

        var tenantBucket = _store.GetOrAdd(tenantId, _ => new ConcurrentDictionary<Guid, Alert>());

        var result = tenantBucket.AddOrUpdate(
            alert.Id,
            _ =>
            {
                if (alert.Version != 0)
                {
                    throw new OptimisticLockException(alert.Id.ToString());
                }

                return alert;
            },
            (_, existing) =>
            {
                if (existing.Version + 1 != alert.Version)
                {
                    throw new OptimisticLockException(alert.Id.ToString());
                }

                return alert;
            });

        return Task.FromResult(result);
    }

    public Task<Alert?> FindByIdAsync(string tenantId, Guid alertId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateTenant(tenantId);

        if (!_store.TryGetValue(tenantId, out var tenantBucket))
        {
            return Task.FromResult<Alert?>(null);
        }

        tenantBucket.TryGetValue(alertId, out var alert);
        return Task.FromResult(alert);
    }

    public Task<IReadOnlyCollection<Alert>> FindAllAsync(string tenantId, AlertFilter filter, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateTenant(tenantId);

        if (!_store.TryGetValue(tenantId, out var tenantBucket))
        {
            return Task.FromResult<IReadOnlyCollection<Alert>>(Array.Empty<Alert>());
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

        var result = query
            .OrderByDescending(a => a.CreatedAt)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<Alert>>(result);
    }

    private static void ValidateTenant(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException("Tenant id is required.", nameof(tenantId));
        }
    }
}
