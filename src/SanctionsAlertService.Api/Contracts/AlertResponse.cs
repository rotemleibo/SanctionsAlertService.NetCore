using SanctionsAlertService.Domain.Entities;

namespace SanctionsAlertService.Api.Contracts;

public sealed record AlertResponse(
    Guid Id,
    string TransactionId,
    string MatchedEntityName,
    int MatchScore,
    string Status,
    string? AssignedTo,
    string TenantId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string? DecisionNote)
{
    public static AlertResponse FromDomain(Alert alert) => new(
        Id: alert.Id,
        TransactionId: alert.TransactionId,
        MatchedEntityName: alert.MatchedEntityName,
        MatchScore: alert.MatchScore,
        Status: alert.Status.ToString(),
        AssignedTo: alert.AssignedTo,
        TenantId: alert.TenantId,
        CreatedAt: alert.CreatedAt,
        UpdatedAt: alert.UpdatedAt,
        DecisionNote: alert.DecisionNote);
}
