using SanctionsAlertService.Domain.Entities;
using SanctionsAlertService.Domain.ValueObjects;

namespace SanctionsAlertService.Application.Repositories;

public interface IAlertRepository
{
    Task<Alert?> FindByIdAsync(string tenantId, Guid alertId, CancellationToken cancellationToken = default);
    Task<Alert?> FindByTransactionIdAsync(string tenantId, string transactionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Alert>> FindAllAsync(string tenantId, AlertFilter filter, CancellationToken cancellationToken = default);
}
