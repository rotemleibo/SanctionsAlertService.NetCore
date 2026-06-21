namespace SanctionsAlertService.Domain.Exceptions;

public sealed class TransactionAlreadyExistsException(string transactionId)
    : Exception($"Transaction '{transactionId}' already exists.")
{
}
