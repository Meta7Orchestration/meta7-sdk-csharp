using META7.Operator.Api;
using META7.Operator.Api.Revenue;
using META7.Operator.Api.Revenue.Models;
using META7.Operator.Contracts;
using META7.Operator.Contracts.Revenue;

namespace META7.Operator.Api.Tests;

public sealed class OperatorApiRevenueTests
{
    private static readonly Uri AllowedFormUri = new("https://forms.meta7.example/lead");

    [Theory]
    [InlineData(OperatorActionType.SubmitLeadForm)]
    [InlineData(OperatorActionType.RequestCallback)]
    [InlineData(OperatorActionType.TriggerWebhook)]
    [InlineData(OperatorActionType.CreateSupportTicket)]
    [InlineData(OperatorActionType.RegisterInterest)]
    public async Task EachWriteAction_RoutesToRevenueExecutor(OperatorActionType actionType)
    {
        var fixture = BuildFixture();

        var response = await fixture.Service.ExecuteAsync(
            actionType,
            AllowedFormUri,
            StandardFormData(),
            "corr-001");

        Assert.True(response.IsAllowed);
        Assert.True(response.IsExecuted);
        Assert.Equal(actionType.ToRevenueActionType(), response.ActionType);
        Assert.Equal(1, fixture.PlaywrightExecutor.Invocations);
    }

    [Fact]
    public async Task DomainAllowlist_IsEnforced()
    {
        var fixture = BuildFixture();

        var response = await fixture.Service.ExecuteAsync(
            OperatorActionType.SubmitLeadForm,
            new Uri("https://outside.example/lead"),
            StandardFormData(),
            "corr-002");

        Assert.False(response.IsAllowed);
        Assert.False(response.IsExecuted);
        Assert.Contains("allowlist", response.Reason, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, fixture.PlaywrightExecutor.Invocations);
    }

    [Fact]
    public async Task SafetyGate_RejectsLoginOrPrivateData()
    {
        var fixture = BuildFixture();

        var blocked = await fixture.Executor.ExecuteAsync(new RevenueActionRequest
        {
            ActionType = RevenueActionType.CreateSupportTicket,
            TargetUrl = AllowedFormUri,
            CorrelationId = "corr-003",
            FormData = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["name"] = "Buyer",
                ["email"] = "buyer@example.com",
                ["password"] = "should-not-pass"
            },
            RequiresAuthentication = true,
            ContainsPrivateData = true,
            DirectiveTimeout = TimeSpan.FromSeconds(5)
        });

        Assert.False(blocked.IsAllowed);
        Assert.False(blocked.IsExecuted);
        Assert.Equal(0, fixture.PlaywrightExecutor.Invocations);
    }

    [Fact]
    public async Task Responses_AreDeterministic_ForEquivalentRequest()
    {
        var fixture = BuildFixture();
        var request = new RevenueActionRequest
        {
            ActionType = RevenueActionType.TriggerWebhook,
            TargetUrl = AllowedFormUri,
            CorrelationId = "corr-004",
            FormData = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["name"] = "Pipeline",
                ["email"] = "pipeline@example.com",
                ["message"] = "Webhook trigger"
            },
            DirectiveTimeout = TimeSpan.FromSeconds(5)
        };

        var first = await fixture.Executor.ExecuteAsync(request);
        var second = await fixture.Executor.ExecuteAsync(request);

        Assert.Equal(first, second);
    }

    [Fact]
    public async Task SafeLock_BlocksAllWriteActions()
    {
        var fixture = BuildFixture(isSafeLockActive: true);

        var response = await fixture.Service.ExecuteAsync(
            OperatorActionType.RequestCallback,
            AllowedFormUri,
            StandardFormData(),
            "corr-005");

        Assert.False(response.IsAllowed);
        Assert.Contains("SAFE_LOCK", response.Reason, StringComparison.Ordinal);
        Assert.Equal(0, fixture.PlaywrightExecutor.Invocations);
    }

    [Fact]
    public async Task RevenueFlowOrchestrator_BuildsDeterministicActionRequest_AndExecutes()
    {
        var fixture = BuildFixture();
        var orchestrator = new RevenueFlowOrchestrator(new RevenueOpportunityDetector(), fixture.Executor);

        var suggestion = new OutreachSuggestion(
            "Please register for product access",
            0.84,
            "product access enterprise",
            AllowedFormUri);
        var context = new CommunityContext("Enterprise Buyers", 90, 78, true);

        var response = await orchestrator.ExecuteAsync(suggestion, context, "corr-006");

        Assert.True(response.IsAllowed);
        Assert.True(response.IsExecuted);
        Assert.Equal(RevenueActionType.RequestCallback, response.ActionType);
    }

    private static TestFixture BuildFixture(bool isSafeLockActive = false)
    {
        var safeLock = new FakeSafeLockStateProvider(isSafeLockActive);
        var gate = new RevenueSafetyGate(["forms.meta7.example"]);
        var executor = new FakePlaywrightOperatorExecutor();
        var revenueExecutor = new RevenueActionExecutor(safeLock, gate, executor);
        var service = new DirectiveExecutionService(safeLock, gate, revenueExecutor);

        return new TestFixture(service, revenueExecutor, executor);
    }

    private static IReadOnlyDictionary<string, string> StandardFormData() => new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["name"] = "Alex Buyer",
        ["email"] = "alex@example.com",
        ["message"] = "Interested in pricing"
    };

    private sealed record TestFixture(
        DirectiveExecutionService Service,
        RevenueActionExecutor Executor,
        FakePlaywrightOperatorExecutor PlaywrightExecutor);

    private sealed class FakeSafeLockStateProvider(bool isSafeLockActive) : ISafeLockStateProvider
    {
        public bool IsSafeLockActive { get; } = isSafeLockActive;
    }

    private sealed class FakePlaywrightOperatorExecutor : IPlaywrightOperatorExecutor
    {
        public int Invocations { get; private set; }

        public Task<RevenueActionResult> ExecuteWriteActionAsync(RevenueActionRequest request, CancellationToken cancellationToken)
        {
            Invocations++;
            return Task.FromResult(new RevenueActionResult(
                ExecutionReference: $"EXEC-{request.CorrelationId}-{request.ActionType}",
                ActionDigest: request.TargetUrl.Host.ToLowerInvariant(),
                IsDeterministic: true));
        }
    }
}
