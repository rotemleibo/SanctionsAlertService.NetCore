using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SanctionsAlertService.Api.Tests;

public sealed class AlertsApiTests
{
    [Fact]
    public async Task ListAlerts_WithoutTenantHeader_ReturnsBadRequest()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/alerts");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("TENANT_REQUIRED", error.Code);
    }

    [Fact]
    public async Task GetAlert_ByDifferentTenant_ReturnsNotFound()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var created = await CreateAlertAsync(client, "tenant-a", "txn-isolation-get");
        var response = await SendAsTenantAsync(client, "tenant-b", HttpMethod.Get, $"/api/v1/alerts/{created.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("ALERT_NOT_FOUND", error.Code);
    }

    [Fact]
    public async Task EscalateAlert_ByDifferentTenant_ReturnsNotFound()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var created = await CreateAlertAsync(client, "tenant-a", "txn-isolation-escalate");
        var response = await SendAsTenantAsync(client, "tenant-b", HttpMethod.Post, $"/api/v1/alerts/{created.Id}/escalation");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("ALERT_NOT_FOUND", error.Code);
    }

    [Fact]
    public async Task DecideAlert_ByDifferentTenant_ReturnsNotFound()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var created = await CreateAlertAsync(client, "tenant-a", "txn-isolation-decide");
        var response = await SendAsTenantAsync(
            client,
            "tenant-b",
            HttpMethod.Post,
            $"/api/v1/alerts/{created.Id}/decision",
            new { decision = "CLEARED", decisionNote = "reviewed" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("ALERT_NOT_FOUND", error.Code);
    }

    [Fact]
    public async Task ListAlerts_WhenTenantHasNoAccessToOtherTenantAlerts_ReturnsOnlyOwnAlerts()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var tenantAAlert = await CreateAlertAsync(client, "tenant-a", "txn-list-a");
        _ = await CreateAlertAsync(client, "tenant-b", "txn-list-b");

        var response = await SendAsTenantAsync(client, "tenant-a", HttpMethod.Get, "/api/v1/alerts");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var alerts = await response.Content.ReadFromJsonAsync<AlertResponse[]>();
        var typedAlerts = Assert.IsType<AlertResponse[]>(alerts);

        Assert.Single(typedAlerts);
        Assert.Equal(tenantAAlert.Id, typedAlerts[0].Id);
        Assert.Equal("tenant-a", typedAlerts[0].TenantId);
    }

    [Fact]
    public async Task ListAlerts_WithMinMatchScoreOutOfRange_ReturnsValidationError()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var response = await SendAsTenantAsync(client, "tenant-1", HttpMethod.Get, "/api/v1/alerts?minMatchScore=101");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("VALIDATION_ERROR", error.Code);
    }

    [Fact]
    public async Task CreateAlert_WithDuplicateTransactionIdInSameTenant_ReturnsConflict()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        _ = await CreateAlertAsync(client, "tenant-1", "txn-duplicate");
        var duplicateResponse = await SendAsTenantAsync(
            client,
            "tenant-1",
            HttpMethod.Post,
            "/api/v1/alerts",
            new
            {
                transactionId = "txn-duplicate",
                matchedEntityName = "Acme",
                matchScore = 87,
                assignedTo = "analyst-2"
            });

        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);

        var error = await duplicateResponse.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("TRANSACTION_ALREADY_EXISTS", error.Code);
    }

    [Fact]
    public async Task DecideAlert_Twice_ReturnsConflictOnSecondAttempt()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var created = await CreateAlertAsync(client, "tenant-1", "txn-decide-twice");

        var firstDecision = await SendAsTenantAsync(
            client,
            "tenant-1",
            HttpMethod.Post,
            $"/api/v1/alerts/{created.Id}/decision",
            new { decision = "CLEARED", decisionNote = "false positive" });

        Assert.Equal(HttpStatusCode.OK, firstDecision.StatusCode);

        var secondDecision = await SendAsTenantAsync(
            client,
            "tenant-1",
            HttpMethod.Post,
            $"/api/v1/alerts/{created.Id}/decision",
            new { decision = "CONFIRMED_HIT", decisionNote = "retry" });

        Assert.Equal(HttpStatusCode.Conflict, secondDecision.StatusCode);

        var error = await secondDecision.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("DECISION_ALREADY_MADE", error.Code);
    }

    [Fact]
    public async Task EscalateAlert_WhenNotOpen_ReturnsInvalidStateTransition()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var created = await CreateAlertAsync(client, "tenant-1", "txn-invalid-escalate");

        var decided = await SendAsTenantAsync(
            client,
            "tenant-1",
            HttpMethod.Post,
            $"/api/v1/alerts/{created.Id}/decision",
            new { decision = "CLEARED", decisionNote = "done" });
        decided.EnsureSuccessStatusCode();

        var escalateResponse = await SendAsTenantAsync(client, "tenant-1", HttpMethod.Post, $"/api/v1/alerts/{created.Id}/escalation");

        Assert.Equal(HttpStatusCode.Conflict, escalateResponse.StatusCode);

        var error = await escalateResponse.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("INVALID_STATE_TRANSITION", error.Code);
    }

    private static async Task<AlertResponse> CreateAlertAsync(HttpClient client, string tenantId, string transactionId)
    {
        var response = await SendAsTenantAsync(
            client,
            tenantId,
            HttpMethod.Post,
            "/api/v1/alerts",
            new
            {
                transactionId,
                matchedEntityName = "Acme",
                matchScore = 87,
                assignedTo = "analyst-1"
            });

        response.EnsureSuccessStatusCode();

        var alert = await response.Content.ReadFromJsonAsync<AlertResponse>();
        return Assert.IsType<AlertResponse>(alert);
    }

    private static Task<HttpResponseMessage> SendAsTenantAsync(
        HttpClient client,
        string tenantId,
        HttpMethod method,
        string uri,
        object? body = null)
    {
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Add("X-Tenant-Id", tenantId);

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return client.SendAsync(request);
    }

    private sealed record AlertResponse(Guid Id, string TransactionId, string Status, string TenantId, string? DecisionNote);

    private sealed record ApiErrorResponse(
        DateTimeOffset Timestamp,
        int Status,
        string Code,
        string Message,
        string Path,
        string[] Details);
}
