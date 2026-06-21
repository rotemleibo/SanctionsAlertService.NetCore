using SanctionsAlertService.Domain.Enums;

namespace SanctionsAlertService.Domain.ValueObjects;

public sealed record AlertFilter(AlertStatus? Status, int? MinMatchScore);
