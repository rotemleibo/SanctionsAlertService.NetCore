namespace SanctionsAlertService.Domain.Exceptions;

public sealed class OptimisticLockException(string alertId)
    : Exception($"Alert '{alertId}' was modified by another request.")
{
}
