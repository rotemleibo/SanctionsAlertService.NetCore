using SanctionsAlertService.Application.Outbox;
using SanctionsAlertService.Application.Repositories;
using SanctionsAlertService.Domain.Entities;
using SanctionsAlertService.Domain.Enums;
using SanctionsAlertService.Domain.Events;
using SanctionsAlertService.Domain.Exceptions;
using SanctionsAlertService.Domain.ValueObjects;

namespace SanctionsAlertService.Application.Services;

public sealed class AlertService(IAlertRepository repository, IAlertUnitOfWork unitOfWork)
{
    private static readonly IReadOnlyCollection<AlertEvent> NoEvents = [];

    public async Task<Alert> CreateAlertAsync(
        string tenantId,
        string transactionId,
        string matchedEntityName,
        int matchScore,
        string? assignedTo,
        CancellationToken cancellationToken = default)
    {
        var existing = await repository.FindByTransactionIdAsync(tenantId, transactionId, cancellationToken);
        if (existing is not null)
        {
            throw new TransactionAlreadyExistsException(transactionId.Trim());
        }

        var now = DateTimeOffset.UtcNow;
        var alert = Alert.Create(tenantId, transactionId, matchedEntityName, matchScore, assignedTo, now);
        return await unitOfWork.SaveAsync(tenantId, alert, NoEvents, cancellationToken);
    }

    public Task<IReadOnlyCollection<Alert>> ListAlertsAsync(
        string tenantId,
        AlertStatus? status,
        int? minMatchScore,
        CancellationToken cancellationToken = default)
    {
        if (minMatchScore is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(minMatchScore), "minMatchScore must be between 0 and 100.");
        }

        return repository.FindAllAsync(tenantId, new AlertFilter(status, minMatchScore), cancellationToken);
    }

    public async Task<Alert> GetByIdAsync(string tenantId, Guid alertId, CancellationToken cancellationToken = default)
    {
        var alert = await repository.FindByIdAsync(tenantId, alertId, cancellationToken);
        return alert ?? throw new AlertNotFoundException(alertId.ToString());
    }

    public async Task<Alert> EscalateAsync(string tenantId, Guid alertId, CancellationToken cancellationToken = default)
    {
        var current = await GetByIdAsync(tenantId, alertId, cancellationToken);
        var updated = current.Escalate(DateTimeOffset.UtcNow);

        var evt = new AlertEscalated(
            EventId: Guid.NewGuid(),
            AlertId: updated.Id,
            TenantId: updated.TenantId,
            Outcome: AlertStatus.ESCALATED.ToString(),
            PreviousStatus: current.Status.ToString(),
            Timestamp: DateTimeOffset.UtcNow);

        return await unitOfWork.SaveAsync(tenantId, updated, [evt], cancellationToken);
    }

    public async Task<Alert> DecideAsync(
        string tenantId,
        Guid alertId,
        DecisionOutcome outcome,
        string note,
        CancellationToken cancellationToken = default)
    {
        var current = await GetByIdAsync(tenantId, alertId, cancellationToken);
        var updated = current.Decide(new Decision(outcome, note), DateTimeOffset.UtcNow);

        var evt = new AlertDecided(
            EventId: Guid.NewGuid(),
            AlertId: updated.Id,
            TenantId: updated.TenantId,
            Decision: outcome,
            Timestamp: DateTimeOffset.UtcNow);

        return await unitOfWork.SaveAsync(tenantId, updated, [evt], cancellationToken);
    }
}
