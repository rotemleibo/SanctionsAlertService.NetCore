namespace SanctionsAlertService.Domain.Exceptions;

public sealed class AlertNotFoundException(string alertId) : Exception($"Alert '{alertId}' was not found.")
{
}
