using SanctionsAlertService.Application.Events;
using SanctionsAlertService.Application.Repositories;
using SanctionsAlertService.Domain.Entities;
using SanctionsAlertService.Domain.Enums;
using SanctionsAlertService.Domain.Events;
using SanctionsAlertService.Domain.Exceptions;
using SanctionsAlertService.Domain.ValueObjects;

namespace SanctionsAlertService.Application.Services;

public sealed class AlertService(IAlertRepository repository, IAlertEventPublisher eventPublisher)
{
    public async Task<Alert> CreateAlertAsync(
        string tenantId,
        string transactionId,
        string matchedEntityName,
        int matchScore,
        string? assignedTo,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var alert = Alert.Create(tenantId, transactionId, matchedEntityName, matchScore, assignedTo, now);
        return await repository.SaveAsync(tenantId, alert, cancellationToken);
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
        var persisted = await repository.SaveAsync(tenantId, updated, cancellationToken);

        var evt = new AlertEscalated(
            EventId: Guid.NewGuid(),
            AlertId: persisted.Id,
            TenantId: persisted.TenantId,
            Outcome: AlertStatus.ESCALATED.ToString(),
            PreviousStatus: current.Status.ToString(),
            Timestamp: DateTimeOffset.UtcNow);

        await TryPublishAsync(evt, cancellationToken);
        return persisted;
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
        var persisted = await repository.SaveAsync(tenantId, updated, cancellationToken);

        var evt = new AlertDecided(
            EventId: Guid.NewGuid(),
            AlertId: persisted.Id,
            TenantId: persisted.TenantId,
            Decision: outcome,
            Timestamp: DateTimeOffset.UtcNow);

        await TryPublishAsync(evt, cancellationToken);
        return persisted;
    }

    private async Task TryPublishAsync(AlertEvent evt, CancellationToken cancellationToken)
    {
        try
        {
            await eventPublisher.PublishAsync(evt, cancellationToken);
        }
        catch
        {
            // v1 policy: state is already persisted; publish failure does not fail request
        }
    }
}
