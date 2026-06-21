using SanctionsAlertService.Api.Errors;

namespace SanctionsAlertService.Api.Tenant;

public sealed class TenantMiddleware(RequestDelegate next)
{
    public const string TenantHeader = "X-Tenant-Id";

    public async Task InvokeAsync(HttpContext context, TenantContext tenantContext)
    {
        var tenantId = context.Request.Headers[TenantHeader].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            var error = new ApiError(
                Timestamp: DateTimeOffset.UtcNow,
                Status: StatusCodes.Status400BadRequest,
                Code: "TENANT_REQUIRED",
                Message: "X-Tenant-Id header is required.",
                Path: context.Request.Path,
                Details: Array.Empty<string>());

            await context.Response.WriteAsJsonAsync(error);
            return;
        }

        tenantContext.TenantId = tenantId.Trim();
        await next(context);
    }
}
