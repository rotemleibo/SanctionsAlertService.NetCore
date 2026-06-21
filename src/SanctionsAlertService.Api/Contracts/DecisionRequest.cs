using System.ComponentModel.DataAnnotations;
using SanctionsAlertService.Domain.Enums;

namespace SanctionsAlertService.Api.Contracts;

public sealed class DecisionRequest
{
    [Required]
    public DecisionOutcome Decision { get; init; }

    [Required]
    public string DecisionNote { get; init; } = string.Empty;
}
