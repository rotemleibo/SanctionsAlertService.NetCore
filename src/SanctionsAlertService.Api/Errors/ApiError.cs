namespace SanctionsAlertService.Api.Errors;

public sealed record ApiError(
    DateTimeOffset Timestamp,
    int Status,
    string Code,
    string Message,
    string Path,
    IReadOnlyCollection<string> Details);
