using SanctionsAlertService.Domain.Enums;

namespace SanctionsAlertService.Domain.Events;

public sealed record AlertDecided(
    Guid EventId,
    Guid AlertId,
    string TenantId,
    DecisionOutcome Decision,
    DateTimeOffset Timestamp)
    : AlertEvent(EventId, "alert.decided", AlertId, TenantId, Timestamp);
