namespace SanctionsAlertService.Infrastructure.Tests.TestDoubles;

public sealed class TestTimeProvider(DateTimeOffset start) : TimeProvider
{
    private DateTimeOffset _now = start;

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan delta) => _now += delta;

    public void Set(DateTimeOffset value) => _now = value;
}
