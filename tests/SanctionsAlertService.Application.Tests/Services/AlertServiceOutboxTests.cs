using SanctionsAlertService.Application.Outbox;
using SanctionsAlertService.Application.Services;
using SanctionsAlertService.Domain.Enums;
using SanctionsAlertService.Infrastructure.Outbox;
using SanctionsAlertService.Infrastructure.Persistence;
using SanctionsAlertService.Infrastructure.Repositories;

namespace SanctionsAlertService.Application.Tests.Services;

public sealed class AlertServiceOutboxTests
{
    private const string TenantId = "tenant-1";

    [Fact]
    public async Task EscalateAsync_PersistsAlert_AndEnqueuesEscalatedOutboxMessage()
    {
        var context = new TestContext();
        var created = await context.Service.CreateAlertAsync(TenantId, "txn-1", "Acme", 90, null);

        var escalated = await context.Service.EscalateAsync(TenantId, created.Id);

        Assert.Equal(AlertStatus.ESCALATED, escalated.Status);

        var message = Assert.Single(context.Database.GetOutboxMessages());
        Assert.Equal("alert.escalated", message.EventType);
        Assert.Equal(OutboxMessageStatus.Pending, message.Status);
        Assert.Equal(TenantId, message.TenantId);
    }

    [Fact]
    public async Task DecideAsync_PersistsAlert_AndEnqueuesDecidedOutboxMessage()
    {
        var context = new TestContext();
        var created = await context.Service.CreateAlertAsync(TenantId, "txn-1", "Acme", 90, null);

        var decided = await context.Service.DecideAsync(TenantId, created.Id, DecisionOutcome.CLEARED, "looks fine");

        Assert.Equal(AlertStatus.CLEARED, decided.Status);

        var message = Assert.Single(context.Database.GetOutboxMessages());
        Assert.Equal("alert.decided", message.EventType);
        Assert.Equal(OutboxMessageStatus.Pending, message.Status);
    }

    [Fact]
    public async Task CreateAlertAsync_DoesNotEnqueueOutboxMessage()
    {
        var context = new TestContext();

        await context.Service.CreateAlertAsync(TenantId, "txn-1", "Acme", 90, null);

        Assert.Empty(context.Database.GetOutboxMessages());
    }

    private sealed class TestContext
    {
        public TestContext()
        {
            Database = new InMemoryDatabase();
            var repository = new InMemoryAlertRepository(Database);
            var unitOfWork = new InMemoryAlertUnitOfWork(Database, new OutboxEventSerializer(), TimeProvider.System);
            Service = new AlertService(repository, unitOfWork);
        }

        public InMemoryDatabase Database { get; }

        public AlertService Service { get; }
    }
}
