using SanctionsAlertService.Domain.Exceptions;

namespace SanctionsAlertService.Api.Errors;

public sealed class GlobalExceptionHandler(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await WriteErrorAsync(context, ex);
        }
    }

    private static Task WriteErrorAsync(HttpContext context, Exception ex)
    {
        var (status, code, message) = ex switch
        {
            AlertNotFoundException => (StatusCodes.Status404NotFound, "ALERT_NOT_FOUND", ex.Message),
            DecisionAlreadyMadeException => (StatusCodes.Status409Conflict, "DECISION_ALREADY_MADE", ex.Message),
            TransactionAlreadyExistsException => (StatusCodes.Status409Conflict, "TRANSACTION_ALREADY_EXISTS", ex.Message),
            InvalidStateTransitionException => (StatusCodes.Status409Conflict, "INVALID_STATE_TRANSITION", ex.Message),
            OptimisticLockException => (StatusCodes.Status409Conflict, "OPTIMISTIC_LOCK_CONFLICT", ex.Message),
            ArgumentOutOfRangeException => (StatusCodes.Status400BadRequest, "VALIDATION_ERROR", ex.Message),
            ArgumentException => (StatusCodes.Status400BadRequest, "VALIDATION_ERROR", ex.Message),
            _ => (StatusCodes.Status500InternalServerError, "INTERNAL_ERROR", "An unexpected error occurred.")
        };

        context.Response.StatusCode = status;
        var error = new ApiError(
            Timestamp: DateTimeOffset.UtcNow,
            Status: status,
            Code: code,
            Message: message,
            Path: context.Request.Path,
            Details: Array.Empty<string>());

        return context.Response.WriteAsJsonAsync(error);
    }
}
