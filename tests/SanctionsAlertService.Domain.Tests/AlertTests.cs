using SanctionsAlertService.Domain.Entities;
using SanctionsAlertService.Domain.Enums;
using SanctionsAlertService.Domain.Exceptions;
using SanctionsAlertService.Domain.ValueObjects;

namespace SanctionsAlertService.Domain.Tests;

public sealed class AlertTests
{
    private static readonly DateTimeOffset CreatedAtUtc = new(2025, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset UpdatedAtUtc = CreatedAtUtc.AddMinutes(5);

    [Fact]
    public void Create_SetsOpenStatus_AndInitialTimestamps()
    {
        var alert = Alert.Create("tenant-1", "txn-1", "Acme", 87, "analyst-1", CreatedAtUtc);

        Assert.Equal(AlertStatus.OPEN, alert.Status);
        Assert.Equal(CreatedAtUtc, alert.CreatedAt);
        Assert.Equal(CreatedAtUtc, alert.UpdatedAt);
        Assert.Null(alert.DecisionNote);
        Assert.Equal(0, alert.Version);
    }

    [Fact]
    public void Escalate_FromOpen_TransitionsToEscalated_AndIncrementsVersion()
    {
        var alert = Alert.Create("tenant-1", "txn-1", "Acme", 87, null, CreatedAtUtc);

        var escalated = alert.Escalate(UpdatedAtUtc);

        Assert.Equal(AlertStatus.ESCALATED, escalated.Status);
        Assert.Equal(UpdatedAtUtc, escalated.UpdatedAt);
        Assert.Equal(1, escalated.Version);
    }

    [Fact]
    public void Escalate_FromEscalated_ThrowsInvalidStateTransition()
    {
        var alert = Alert.Create("tenant-1", "txn-1", "Acme", 87, null, CreatedAtUtc)
            .Escalate(UpdatedAtUtc);

        Assert.Throws<InvalidStateTransitionException>(() => alert.Escalate(UpdatedAtUtc.AddMinutes(1)));
    }

    [Fact]
    public void Decide_FromEscalated_AllowsConfirmedHit_AndStoresTrimmedNote()
    {
        var alert = Alert.Create("tenant-1", "txn-1", "Acme", 87, null, CreatedAtUtc)
            .Escalate(UpdatedAtUtc);

        var decided = alert.Decide(new Decision(DecisionOutcome.CONFIRMED_HIT, "  true hit  "), UpdatedAtUtc.AddMinutes(1));

        Assert.Equal(AlertStatus.CONFIRMED_HIT, decided.Status);
        Assert.Equal("true hit", decided.DecisionNote);
        Assert.Equal(2, decided.Version);
    }

    [Fact]
    public void Decide_WhenAlreadyDecided_ThrowsDecisionAlreadyMade()
    {
        var alert = Alert.Create("tenant-1", "txn-1", "Acme", 87, null, CreatedAtUtc)
            .Decide(new Decision(DecisionOutcome.CLEARED, "looks fine"), UpdatedAtUtc);

        Assert.Throws<DecisionAlreadyMadeException>(() =>
            alert.Decide(new Decision(DecisionOutcome.CONFIRMED_HIT, "retry"), UpdatedAtUtc.AddMinutes(1)));
    }

    [Fact]
    public void Decide_WithEmptyNote_ThrowsArgumentException()
    {
        var alert = Alert.Create("tenant-1", "txn-1", "Acme", 87, null, CreatedAtUtc);

        Assert.Throws<ArgumentException>(() =>
            alert.Decide(new Decision(DecisionOutcome.CLEARED, "   "), UpdatedAtUtc));
    }
}
