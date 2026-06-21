namespace SanctionsAlertService.Application.Outbox;

public enum OutboxMessageStatus
{
    Pending,
    Processing,
    Processed,
    DeadLetter
}
