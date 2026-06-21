namespace SanctionsAlertService.Domain.Events;

public sealed record AlertEscalated(
    Guid EventId,
    Guid AlertId,
    string TenantId,
    string Outcome,
    string PreviousStatus,
    DateTimeOffset Timestamp)
    : AlertEvent(EventId, "alert.escalated", AlertId, TenantId, Timestamp);
