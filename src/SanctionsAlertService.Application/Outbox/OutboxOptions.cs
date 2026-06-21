namespace SanctionsAlertService.Application.Outbox;

public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(5);

    public int BatchSize { get; set; } = 50;

    public int MaxAttempts { get; set; } = 10;

    public TimeSpan BaseBackoff { get; set; } = TimeSpan.FromSeconds(5);

    public TimeSpan MaxBackoff { get; set; } = TimeSpan.FromMinutes(10);

    public TimeSpan LeaseDuration { get; set; } = TimeSpan.FromSeconds(30);
}
