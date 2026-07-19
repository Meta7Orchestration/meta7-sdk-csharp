using System.Net;
using System.Net.Http.Json;
using META7.Operator.Api;
using META7.Operator.Api.Execution;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace META7.Operator.Api.Tests;

public class OperatorApiBrowserTests
{
    [Fact]
    public async Task Navigate_Returns_Deterministic_Result()
    {
        using var app = new OperatorApiFactory();
        var client = app.CreateClient();

        var response = await client.PostAsJsonAsync("/operator/execute", new OperatorDirective(
            OperatorActionType.Navigate,
            Url: "https://allowed.test/page",
            TimeoutMs: 3000));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<OperatorResult>();
        Assert.NotNull(body);
        Assert.True(body.Succeeded);
        Assert.Equal("https://allowed.test/page", body.Data["url"]?.ToString());
    }

    [Fact]
    public async Task ReadPage_Returns_Deterministic_Content()
    {
        using var app = new OperatorApiFactory();
        var client = app.CreateClient();

        var response = await client.PostAsJsonAsync("/operator/execute", new OperatorDirective(
            OperatorActionType.ReadPage,
            Url: "https://allowed.test/read"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<OperatorResult>();
        Assert.NotNull(body);
        Assert.Equal("Deterministic Title", body.Data["title"]?.ToString());
        Assert.Equal("Deterministic page body", body.Data["textContent"]?.ToString());
    }

    [Fact]
    public async Task ExtractStructuredData_Returns_Selected_Data()
    {
        using var app = new OperatorApiFactory();
        var client = app.CreateClient();

        var response = await client.PostAsJsonAsync("/operator/execute", new OperatorDirective(
            OperatorActionType.ExtractStructuredData,
            Url: "https://allowed.test/extract",
            Selectors: new Dictionary<string, string>
            {
                ["headline"] = "h1",
                ["price"] = ".price"
            }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<OperatorResult>();
        Assert.NotNull(body);

        var data = Assert.IsAssignableFrom<System.Text.Json.JsonElement>(body.Data["data"]);
        Assert.Equal("Synthetic Product", data.GetProperty("headline").GetString());
        Assert.Equal("42.00", data.GetProperty("price").GetString());
    }

    [Fact]
    public async Task WaitForElement_Returns_Found_When_Element_Exists()
    {
        using var app = new OperatorApiFactory();
        var client = app.CreateClient();

        var response = await client.PostAsJsonAsync("/operator/execute", new OperatorDirective(
            OperatorActionType.WaitForElement,
            Url: "https://allowed.test/wait",
            Selector: "#ready",
            TimeoutMs: 200));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<OperatorResult>();
        Assert.NotNull(body);
        Assert.True(body.Succeeded);
    }

    [Fact]
    public async Task TakeScreenshot_Returns_Artifact()
    {
        using var app = new OperatorApiFactory();
        var client = app.CreateClient();

        var response = await client.PostAsJsonAsync("/operator/execute", new OperatorDirective(
            OperatorActionType.TakeScreenshot,
            Url: "https://allowed.test/screenshot"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<OperatorResult>();
        Assert.NotNull(body);
        Assert.Single(body.Artifacts);
        Assert.Equal("image/png", body.Artifacts[0].ContentType);
    }

    [Fact]
    public async Task Disallowed_Domain_Is_Rejected()
    {
        using var app = new OperatorApiFactory();
        var client = app.CreateClient();

        var response = await client.PostAsJsonAsync("/operator/execute", new OperatorDirective(
            OperatorActionType.Navigate,
            Url: "https://forbidden.test/home"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<OperatorResult>();
        Assert.NotNull(body);
        Assert.Contains("allowlist", body.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SafeLock_Rejection_Is_Enforced()
    {
        using var app = new OperatorApiFactory();
        app.SafeLock.IsLocked = true;
        var client = app.CreateClient();

        var response = await client.PostAsJsonAsync("/operator/execute", new OperatorDirective(
            OperatorActionType.ReadPage,
            Url: "https://allowed.test/read"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<OperatorResult>();
        Assert.NotNull(body);
        Assert.Contains("SAFE_LOCK", body.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Timeout_Is_Enforced()
    {
        using var app = new OperatorApiFactory();
        var client = app.CreateClient();

        var response = await client.PostAsJsonAsync("/operator/execute", new OperatorDirective(
            OperatorActionType.WaitForElement,
            Url: "https://allowed.test/wait",
            Selector: "#slow",
            TimeoutMs: 30));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<OperatorResult>();
        Assert.NotNull(body);
        Assert.Contains("timed out", body.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class OperatorApiFactory : WebApplicationFactory<Program>
    {
        public MutableSafeLockState SafeLock { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ISafeLockState>();
                services.RemoveAll<IBrowserBootstrap>();

                services.AddSingleton<ISafeLockState>(SafeLock);
                services.AddSingleton<IBrowserBootstrap, FakeBrowserBootstrap>();
                services.Configure<OperatorExecutionOptions>(options =>
                {
                    options.AllowedDomains = ["allowed.test", "localhost", "127.0.0.1"];
                    options.DefaultTimeoutMs = 100;
                    options.MaxTimeoutMs = 1000;
                });
            });
        }
    }

    private sealed class MutableSafeLockState : ISafeLockState
    {
        public bool IsLocked { get; set; }
    }

    private sealed class FakeBrowserBootstrap : IBrowserBootstrap
    {
        public Task<IReadOnlyBrowserSession> CreateSessionAsync(OperatorExecutionOptions options, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyBrowserSession>(new FakeBrowserSession());
    }

    private sealed class FakeBrowserSession : IReadOnlyBrowserSession
    {
        private string _url = "about:blank";

        public Task NavigateAsync(string url, CancellationToken cancellationToken)
        {
            _url = url;
            return Task.CompletedTask;
        }

        public Task<(string Url, string Title, string TextContent)> ReadPageAsync(CancellationToken cancellationToken)
            => Task.FromResult((_url, "Deterministic Title", "Deterministic page body"));

        public Task<Dictionary<string, string?>> ExtractStructuredDataAsync(IReadOnlyDictionary<string, string> selectors, CancellationToken cancellationToken)
        {
            var values = new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["h1"] = "Synthetic Product",
                [".price"] = "42.00"
            };

            var result = selectors.ToDictionary(s => s.Key, s => values.TryGetValue(s.Value, out var value) ? value : null, StringComparer.Ordinal);
            return Task.FromResult(result);
        }

        public async Task<bool> WaitForElementAsync(string selector, TimeSpan timeout, CancellationToken cancellationToken)
        {
            if (selector == "#slow")
            {
                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            }

            return selector == "#ready";
        }

        public Task<string> TakeScreenshotBase64Async(CancellationToken cancellationToken)
            => Task.FromResult(Convert.ToBase64String(new byte[] { 1, 2, 3, 4 }));

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
