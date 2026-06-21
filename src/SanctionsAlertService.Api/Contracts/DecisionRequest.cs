using System.ComponentModel.DataAnnotations;
using SanctionsAlertService.Domain.Enums;

namespace SanctionsAlertService.Api.Contracts;

public sealed class DecisionRequest
{
    [Required]
    public DecisionOutcome Decision { get; init; }

    [Required]
    [RegularExpression(@".*\S.*", ErrorMessage = "DecisionNote must not be empty or whitespace.")]
    public string DecisionNote { get; init; } = string.Empty;
}
