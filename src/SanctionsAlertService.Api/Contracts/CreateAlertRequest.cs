using System.ComponentModel.DataAnnotations;

namespace SanctionsAlertService.Api.Contracts;

public sealed class CreateAlertRequest
{
    [Required]
    public string TransactionId { get; init; } = string.Empty;

    [Required]
    public string MatchedEntityName { get; init; } = string.Empty;

    [Range(0, 100)]
    public int MatchScore { get; init; }

    public string? AssignedTo { get; init; }
}
