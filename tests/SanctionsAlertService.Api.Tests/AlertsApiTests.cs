using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
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
    public async Task DecideAlert_Twice_ReturnsConflictOnSecondAttempt()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "tenant-1");

        var created = await CreateAlertAsync(client, "txn-1");

        var firstDecision = await client.PostAsJsonAsync(
            $"/api/v1/alerts/{created.Id}/decision",
            new { decision = "CLEARED", decisionNote = "false positive" });

        Assert.Equal(HttpStatusCode.OK, firstDecision.StatusCode);

        var secondDecision = await client.PostAsJsonAsync(
            $"/api/v1/alerts/{created.Id}/decision",
            new { decision = "CONFIRMED_HIT", decisionNote = "retry" });

        Assert.Equal(HttpStatusCode.Conflict, secondDecision.StatusCode);

        var error = await secondDecision.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("DECISION_ALREADY_MADE", error.Code);
    }

    [Fact]
    public async Task GetAlert_ByDifferentTenant_ReturnsNotFound()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "tenant-1");

        var created = await CreateAlertAsync(client, "txn-2");

        client.DefaultRequestHeaders.Remove("X-Tenant-Id");
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "tenant-2");

        var response = await client.GetAsync($"/api/v1/alerts/{created.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("ALERT_NOT_FOUND", error.Code);
    }

    private static async Task<AlertResponse> CreateAlertAsync(HttpClient client, string transactionId)
    {
        var response = await client.PostAsJsonAsync("/api/v1/alerts", new
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

    private sealed record AlertResponse(Guid Id, string Status, string TenantId, string? DecisionNote);

    private sealed record ApiErrorResponse(
        DateTimeOffset Timestamp,
        int Status,
        string Code,
        string Message,
        string Path,
        string[] Details);
}
