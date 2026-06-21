namespace SanctionsAlertService.Domain.Exceptions;

public sealed class DecisionAlreadyMadeException(string alertId)
    : Exception($"Alert '{alertId}' already has a final decision.")
{
}
