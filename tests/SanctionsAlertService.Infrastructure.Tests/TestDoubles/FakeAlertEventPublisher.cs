using SanctionsAlertService.Application.Events;
using SanctionsAlertService.Domain.Events;

namespace SanctionsAlertService.Infrastructure.Tests.TestDoubles;

public sealed class FakeAlertEventPublisher : IAlertEventPublisher
{
    private readonly Func<AlertEvent, bool> _shouldThrow;

    public FakeAlertEventPublisher(Func<AlertEvent, bool>? shouldThrow = null)
    {
        _shouldThrow = shouldThrow ?? (_ => false);
    }

    public List<AlertEvent> Published { get; } = [];

    public int Attempts { get; private set; }

    public Task PublishAsync(AlertEvent evt, CancellationToken cancellationToken = default)
    {
        Attempts++;

        if (_shouldThrow(evt))
        {
            throw new InvalidOperationException("Simulated publish failure.");
        }

        Published.Add(evt);
        return Task.CompletedTask;
    }
}
