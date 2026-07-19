using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using META7.Operator.Api.Outreach;
using META7.Operator.Contracts.Outreach;
using Xunit;

namespace META7.Operator.Api.Tests;

public class OperatorApiOutreachTests
{
    [Fact]
    public async Task SignalDetection_FindsSignalsDeterministically()
    {
        var detector = new OutreachSignalDetector(
            new FakePlaywrightOperatorExecutor(new ReadOnlyPageSnapshot(
                new Uri("https://seller.example.com/thread"),
                "ระบบล่ม แจ้งเตือน ออเดอร์ตกหล่น need help",
                new Dictionary<string, string> { ["title"] = "POS ช้า issue" },
                ["urgent alert", "orders missing"])),
            ["example.com"]);

        var first = await detector.DetectAsync("https://seller.example.com/thread");
        var second = await detector.DetectAsync("https://seller.example.com/thread");

        Assert.Equal(first, second);
        Assert.Equal(new[] { "need help", "แจ้งเตือน", "ระบบล่ม", "POS ช้า", "ออเดอร์ตกหล่น" }, first.Select(s => s.MatchedKeyword).Distinct().ToArray());
    }

    [Fact]
    public async Task SignalDetection_EnforcesDomainAllowlist()
    {
        var detector = new OutreachSignalDetector(
            new FakePlaywrightOperatorExecutor(ReadOnlyPageSnapshotFor("https://allowed.example.com/x")),
            ["allowed.example.com"]);

        await Assert.ThrowsAsync<InvalidOperationException>(() => detector.DetectAsync("https://blocked.example.net/thread"));
    }

    [Fact]
    public void ContextAnalyzer_IdentifiesPainPointsUrgencyAndCategory()
    {
        var analyzer = new OutreachContextAnalyzer();
        var signals = new List<OutreachSignal>
        {
            TestSignal("https://market-seller.example.com/a", "ระบบล่ม"),
            TestSignal("https://market-seller.example.com/a", "ออเดอร์ตกหล่น")
        };

        var context = analyzer.Analyze(signals);

        Assert.Equal(OutreachUrgencyLevel.Critical, context.UrgencyLevel);
        Assert.Equal("MarketplaceSeller", context.DomainCategory);
        Assert.Contains("SystemOutage", context.PainPoints);
        Assert.Contains("DroppedOrders", context.PainPoints);
    }

    [Fact]
    public async Task DirectiveExecution_EnforcesSafeLock()
    {
        var service = BuildDirectiveExecutionService(
            safeLockEnabled: true,
            policyAllowed: true);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ExecuteAsync(OperatorActionType.DetectOutreachSignals, "https://allowed.example.com/thread"));
    }

    [Fact]
    public async Task DirectiveExecution_GeneratesDeterministicSuggestions()
    {
        var service = BuildDirectiveExecutionService(
            safeLockEnabled: false,
            policyAllowed: true);

        var signals = new List<OutreachSignal>
        {
            TestSignal("https://allowed.example.com/thread", "แจ้งเตือน"),
            TestSignal("https://allowed.example.com/thread", "ออเดอร์ตกหล่น")
        };

        var first = (IReadOnlyList<OutreachSuggestion>)await service.ExecuteAsync(
            OperatorActionType.GenerateOutreachSuggestion,
            signals);

        var second = (IReadOnlyList<OutreachSuggestion>)await service.ExecuteAsync(
            OperatorActionType.GenerateOutreachSuggestion,
            signals);

        Assert.Equal(first, second);
        Assert.All(first, suggestion => Assert.InRange(suggestion.ConfidenceScore, 0.0d, 1.0d));
        Assert.All(first, suggestion => Assert.False(string.IsNullOrWhiteSpace(suggestion.RecommendedValueProposition)));
    }

    [Fact]
    public async Task DirectiveExecution_EnforcesPolicyGate()
    {
        var service = BuildDirectiveExecutionService(
            safeLockEnabled: false,
            policyAllowed: false);

        var signals = new List<OutreachSignal>
        {
            TestSignal("https://allowed.example.com/thread", "need help")
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.ExecuteAsync(OperatorActionType.GenerateOutreachSuggestion, signals));
    }

    private static DirectiveExecutionService BuildDirectiveExecutionService(bool safeLockEnabled, bool policyAllowed)
    {
        var detector = new OutreachSignalDetector(
            new FakePlaywrightOperatorExecutor(ReadOnlyPageSnapshotFor("https://allowed.example.com/thread")),
            ["allowed.example.com"]);

        return new DirectiveExecutionService(
            new FakeSafeLockStateProvider(safeLockEnabled),
            new FakeOutreachPolicyGate(policyAllowed),
            detector,
            new OutreachContextAnalyzer(),
            new OutreachValuePropositionEngine(),
            new OutreachSuggestionFormatter());
    }

    private static ReadOnlyPageSnapshot ReadOnlyPageSnapshotFor(string url) =>
        new(
            new Uri(url),
            "need help แจ้งเตือน ระบบล่ม POS ช้า ออเดอร์ตกหล่น",
            new Dictionary<string, string> { ["title"] = "outreach" },
            ["h1: incident"]);

    private static OutreachSignal TestSignal(string url, string keyword) =>
        new(url, keyword, keyword, "OperationalPainPoint", 0.9d, new Dictionary<string, string>());

    private sealed class FakeSafeLockStateProvider(bool safeLockEnabled) : ISafeLockStateProvider
    {
        public bool IsSafeLockActive => safeLockEnabled;
    }

    private sealed class FakeOutreachPolicyGate(bool policyAllowed) : IOutreachPolicyGate
    {
        public bool CanExecute(OperatorActionType actionType, Uri targetUri) => policyAllowed;
    }

    private sealed class FakePlaywrightOperatorExecutor(ReadOnlyPageSnapshot snapshot) : IPlaywrightOperatorExecutor
    {
        public Task<ReadOnlyPageSnapshot> CaptureReadOnlyAsync(
            Uri targetUri,
            IReadOnlyList<ReadOnlyBrowserActionType> actions,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(snapshot with { TargetUri = targetUri });
    }
}
