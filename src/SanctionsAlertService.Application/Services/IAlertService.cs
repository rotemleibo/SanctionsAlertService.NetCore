using SanctionsAlertService.Domain.Entities;
using SanctionsAlertService.Domain.Enums;

namespace SanctionsAlertService.Application.Services;

public interface IAlertService
{
    Task<Alert> CreateAlertAsync(
        string tenantId,
        string transactionId,
        string matchedEntityName,
        int matchScore,
        string? assignedTo,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Alert>> ListAlertsAsync(
        string tenantId,
        AlertStatus? status,
        int? minMatchScore,
        CancellationToken cancellationToken = default);

    Task<Alert> GetByIdAsync(string tenantId, Guid alertId, CancellationToken cancellationToken = default);

    Task<Alert> EscalateAsync(string tenantId, Guid alertId, CancellationToken cancellationToken = default);

    Task<Alert> DecideAsync(
        string tenantId,
        Guid alertId,
        DecisionOutcome outcome,
        string note,
        CancellationToken cancellationToken = default);
}
