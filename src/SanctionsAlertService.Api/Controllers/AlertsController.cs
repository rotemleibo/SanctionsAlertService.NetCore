using Microsoft.AspNetCore.Mvc;
using SanctionsAlertService.Api.Contracts;
using SanctionsAlertService.Api.Tenant;
using SanctionsAlertService.Application.Services;
using SanctionsAlertService.Domain.Enums;

namespace SanctionsAlertService.Api.Controllers;

[ApiController]
[Route("api/v1/alerts")]
public sealed class AlertsController(IAlertService alertService, TenantContext tenantContext) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<AlertResponse>> Create(
        [FromBody] CreateAlertRequest request,
        CancellationToken cancellationToken)
    {
        var alert = await alertService.CreateAlertAsync(
            tenantId: GetTenantId(),
            transactionId: request.TransactionId,
            matchedEntityName: request.MatchedEntityName,
            matchScore: request.MatchScore,
            assignedTo: request.AssignedTo,
            cancellationToken);

        var response = AlertResponse.FromDomain(alert);
        return CreatedAtAction(nameof(GetById), new { id = alert.Id }, response);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<AlertResponse>>> List(
        [FromQuery] AlertStatus? status,
        [FromQuery] int? minMatchScore,
        CancellationToken cancellationToken)
    {
        var alerts = await alertService.ListAlertsAsync(GetTenantId(), status, minMatchScore, cancellationToken);
        return Ok(alerts.Select(AlertResponse.FromDomain).ToArray());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AlertResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var alert = await alertService.GetByIdAsync(GetTenantId(), id, cancellationToken);
        return Ok(AlertResponse.FromDomain(alert));
    }

    [HttpPost("{id:guid}/escalation")]
    public async Task<ActionResult<AlertResponse>> Escalate(Guid id, CancellationToken cancellationToken)
    {
        var alert = await alertService.EscalateAsync(GetTenantId(), id, cancellationToken);
        return Ok(AlertResponse.FromDomain(alert));
    }

    [HttpPost("{id:guid}/decision")]
    public async Task<ActionResult<AlertResponse>> Decide(
        Guid id,
        [FromBody] DecisionRequest request,
        CancellationToken cancellationToken)
    {
        var alert = await alertService.DecideAsync(
            tenantId: GetTenantId(),
            alertId: id,
            outcome: request.Decision,
            note: request.DecisionNote,
            cancellationToken);

        return Ok(AlertResponse.FromDomain(alert));
    }

    private string GetTenantId()
    {
        return tenantContext.TenantId ?? throw new InvalidOperationException("Tenant was not resolved by TenantMiddleware.");
    }
}
