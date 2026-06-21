using SanctionsAlertService.Application.Repositories;
using SanctionsAlertService.Domain.Entities;
using SanctionsAlertService.Domain.ValueObjects;
using SanctionsAlertService.Infrastructure.Persistence;

namespace SanctionsAlertService.Infrastructure.Repositories;

public sealed class InMemoryAlertRepository(InMemoryDatabase database) : IAlertRepository
{
    public Task<Alert> SaveAsync(string tenantId, Alert alert, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var saved = database.SaveAlert(tenantId, alert, []);
        return Task.FromResult(saved);
    }

    public Task<Alert?> FindByIdAsync(string tenantId, Guid alertId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(database.FindAlertById(tenantId, alertId));
    }

    public Task<Alert?> FindByTransactionIdAsync(string tenantId, string transactionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(database.FindAlertByTransactionId(tenantId, transactionId));
    }

    public Task<IReadOnlyCollection<Alert>> FindAllAsync(string tenantId, AlertFilter filter, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(database.FindAlerts(tenantId, filter));
    }
}
