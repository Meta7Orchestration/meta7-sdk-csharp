namespace META7.Operator.Api.Tests;

using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using META7.CaptainM7A;
using META7.Operator.Contracts;
using Xunit;

/// <summary>
/// Deterministic smoke tests for the META7 Operator API.
/// Uses <see cref="WebApplicationFactory{TEntryPoint}"/> so no real ports are opened.
/// </summary>
public sealed class OperatorApiSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public OperatorApiSmokeTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    // ── /health ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Health_ReturnsHealthy()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("healthy", body, StringComparison.OrdinalIgnoreCase);
    }

    // ── /v1/directives — policy rejections ───────────────────────────────────

    [Fact]
    public async Task PostDirective_RejectsExpiredDirective()
    {
        var client = _factory.CreateClient();
        var directive = BuildDirective(
            actionType: OperatorActionType.ReadPage,
            expiresAt: DateTime.UtcNow.AddSeconds(-30),   // already expired
            allowedDomains: new List<string> { "example.com" },
            targetUrl: "https://example.com/page");

        var response = await client.PostAsJsonAsync("/v1/directives", directive);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<OperatorResult>(JsonOptions);
        Assert.NotNull(result);
        Assert.Equal(OperatorExecutionStatus.Expired, result!.Status);
    }

    [Fact]
    public async Task PostDirective_RejectsDomainOutsideAllowlist()
    {
        var client = _factory.CreateClient();
        var directive = BuildDirective(
            actionType: OperatorActionType.Navigate,
            expiresAt: DateTime.UtcNow.AddMinutes(5),
            allowedDomains: new List<string> { "allowed.com" },
            targetUrl: "https://evil.com/attack");

        var response = await client.PostAsJsonAsync("/v1/directives", directive);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<OperatorResult>(JsonOptions);
        Assert.NotNull(result);
        Assert.Equal(OperatorExecutionStatus.PolicyViolation, result!.Status);
    }

    [Fact]
    public async Task PostDirective_RejectsUnsafeAction()
    {
        var client = _factory.CreateClient();
        var directive = BuildDirective(
            actionType: OperatorActionType.ExecuteScript,  // unsafe
            expiresAt: DateTime.UtcNow.AddMinutes(5),
            allowedDomains: new List<string> { "example.com" },
            targetUrl: "https://example.com/page");

        var response = await client.PostAsJsonAsync("/v1/directives", directive);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<OperatorResult>(JsonOptions);
        Assert.NotNull(result);
        Assert.Equal(OperatorExecutionStatus.Rejected, result!.Status);
    }

    // ── /v1/directives — SAFE_LOCK ────────────────────────────────────────────

    [Fact]
    public async Task PostDirective_ReturnsSafeLocked_WhenSafeLockIsActive()
    {
        // Override ISafeLockStateReader to report SAFE_LOCK active
        var lockedFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<ISafeLockStateReader>(new FakeSafeLockReader(isActive: true));
            });
        });

        var client = lockedFactory.CreateClient();
        var directive = BuildDirective(
            actionType: OperatorActionType.ReadPage,
            expiresAt: DateTime.UtcNow.AddMinutes(5),
            allowedDomains: new List<string> { "example.com" },
            targetUrl: "https://example.com/page");

        var response = await client.PostAsJsonAsync("/v1/directives", directive);

        // HTTP 423 Locked
        Assert.Equal(423, (int)response.StatusCode);
    }

    // ── /v1/directives — happy path ───────────────────────────────────────────

    [Fact]
    public async Task PostDirective_ReturnsSucceeded_ForValidReadOnlyDirective()
    {
        var client = _factory.CreateClient();
        var directive = BuildDirective(
            actionType: OperatorActionType.ReadPage,
            expiresAt: DateTime.UtcNow.AddMinutes(5),
            allowedDomains: new List<string> { "example.com" },
            targetUrl: "https://example.com/page");

        var response = await client.PostAsJsonAsync("/v1/directives", directive);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<OperatorResult>(JsonOptions);
        Assert.NotNull(result);
        Assert.Equal(OperatorExecutionStatus.Succeeded, result!.Status);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    private static OperatorDirective BuildDirective(
        OperatorActionType actionType,
        DateTime expiresAt,
        List<string> allowedDomains,
        string targetUrl) => new()
    {
        Id = Guid.NewGuid().ToString("N"),
        ActionType = actionType,
        TargetUrl = targetUrl,
        IssuedAt = DateTime.UtcNow,
        ExpiresAt = expiresAt,
        AllowedDomains = allowedDomains
    };

    /// <summary>In-memory stub for <see cref="ISafeLockStateReader"/>.</summary>
    private sealed class FakeSafeLockReader : ISafeLockStateReader
    {
        private readonly bool _isActive;
        public FakeSafeLockReader(bool isActive) => _isActive = isActive;
        public bool IsSafeLockActive => _isActive;
    }
}
