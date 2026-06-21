using SanctionsAlertService.Domain.Enums;

namespace SanctionsAlertService.Domain.Exceptions;

public sealed class InvalidStateTransitionException(string alertId, AlertStatus from, string action)
    : Exception($"Cannot perform '{action}' on alert '{alertId}' from status '{from}'.")
{
}
