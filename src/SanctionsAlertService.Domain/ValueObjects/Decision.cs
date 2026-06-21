using SanctionsAlertService.Domain.Enums;

namespace SanctionsAlertService.Domain.ValueObjects;

public sealed record Decision(DecisionOutcome Outcome, string Note);
