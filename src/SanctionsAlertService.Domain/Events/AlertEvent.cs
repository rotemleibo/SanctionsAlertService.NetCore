namespace SanctionsAlertService.Domain.Events;

public abstract record AlertEvent(
    Guid EventId,
    string Event,
    Guid AlertId,
    string TenantId,
    DateTimeOffset Timestamp);
