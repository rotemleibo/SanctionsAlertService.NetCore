using SanctionsAlertService.Domain.Enums;
using SanctionsAlertService.Domain.Exceptions;
using SanctionsAlertService.Domain.ValueObjects;

namespace SanctionsAlertService.Domain.Entities;

public sealed record Alert
{
    public Guid Id { get; init; }

    public string TransactionId { get; init; } = string.Empty;

    public string MatchedEntityName { get; init; } = string.Empty;

    public int MatchScore { get; init; }

    public AlertStatus Status { get; init; }

    public string? AssignedTo { get; init; }

    public string TenantId { get; init; } = string.Empty;

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }

    public string? DecisionNote { get; init; }

    public long Version { get; init; }

    public static Alert Create(
        string tenantId,
        string transactionId,
        string matchedEntityName,
        int matchScore,
        string? assignedTo,
        DateTimeOffset nowUtc)
    {
        ValidateCreateInputs(tenantId, transactionId, matchedEntityName, matchScore, nowUtc);

        return new Alert
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId.Trim(),
            TransactionId = transactionId.Trim(),
            MatchedEntityName = matchedEntityName.Trim(),
            MatchScore = matchScore,
            AssignedTo = string.IsNullOrWhiteSpace(assignedTo) ? null : assignedTo.Trim(),
            Status = AlertStatus.OPEN,
            CreatedAt = nowUtc,
            UpdatedAt = nowUtc,
            DecisionNote = null,
            Version = 0
        };
    }

    public Alert Escalate(DateTimeOffset nowUtc)
    {
        if (Status != AlertStatus.OPEN)
        {
            throw new InvalidStateTransitionException(Id.ToString(), Status, "escalate");
        }

        return this with
        {
            Status = AlertStatus.ESCALATED,
            UpdatedAt = nowUtc,
            Version = Version + 1
        };
    }

    public Alert Decide(Decision decision, DateTimeOffset nowUtc)
    {
        if (decision is null)
        {
            throw new ArgumentNullException(nameof(decision));
        }

        if (Status is AlertStatus.CLEARED or AlertStatus.CONFIRMED_HIT)
        {
            throw new DecisionAlreadyMadeException(Id.ToString());
        }

        if (string.IsNullOrWhiteSpace(decision.Note))
        {
            throw new ArgumentException("Decision note is required.", nameof(decision));
        }

        var decidedStatus = decision.Outcome switch
        {
            DecisionOutcome.CLEARED => AlertStatus.CLEARED,
            DecisionOutcome.CONFIRMED_HIT => AlertStatus.CONFIRMED_HIT,
            _ => throw new ArgumentOutOfRangeException(nameof(decision.Outcome), decision.Outcome, "Unsupported decision outcome.")
        };

        return this with
        {
            Status = decidedStatus,
            DecisionNote = decision.Note.Trim(),
            UpdatedAt = nowUtc,
            Version = Version + 1
        };
    }

    private static void ValidateCreateInputs(
        string tenantId,
        string transactionId,
        string matchedEntityName,
        int matchScore,
        DateTimeOffset nowUtc)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException("Tenant id is required.", nameof(tenantId));
        }

        if (string.IsNullOrWhiteSpace(transactionId))
        {
            throw new ArgumentException("Transaction id is required.", nameof(transactionId));
        }

        if (string.IsNullOrWhiteSpace(matchedEntityName))
        {
            throw new ArgumentException("Matched entity name is required.", nameof(matchedEntityName));
        }

        if (matchScore is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(matchScore), "Match score must be between 0 and 100.");
        }

        if (nowUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Timestamp must be UTC.", nameof(nowUtc));
        }
    }
}
