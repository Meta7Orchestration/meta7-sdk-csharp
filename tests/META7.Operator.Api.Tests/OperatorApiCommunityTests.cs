// ══════════════════════════════════════════════════════════════════════════════
// META7 Operator — Community Connector Layer
// OperatorApiCommunityTests: Deterministic integration tests
// Tests: community scanning, context extraction, domain allowlist, SAFE_LOCK
// ══════════════════════════════════════════════════════════════════════════════

using META7.Operator.Api;
using META7.Operator.Api.Community;
using META7.Operator.Api.Community.Models;
using META7.Operator.Contracts.Community;
using Moq;

namespace META7.Operator.Api.Tests;

// ── SAFE_LOCK helpers ─────────────────────────────────────────────────────────

internal sealed class StaticSafeLockProvider(bool isLocked) : ISafeLockProvider
{
    public bool IsSafeLockActive { get; } = isLocked;
}

// ══════════════════════════════════════════════════════════════════════════════
// CommunitySafetyGate Tests
// ══════════════════════════════════════════════════════════════════════════════

public class CommunitySafetyGateTests
{
    private static CommunitySafetyGate BuildGate(bool isLocked = false)
        => new(new StaticSafeLockProvider(isLocked));

    [Theory]
    [InlineData("https://www.facebook.com/groups/example")]
    [InlineData("https://discord.com/channels/123456/789012")]
    [InlineData("https://openchat.line.me/openchat/discover")]
    [InlineData("https://line.me/ti/g2/example")]
    public void CheckNavigation_AllowsApprovedDomains(string url)
    {
        var gate   = BuildGate();
        var result = gate.CheckNavigation(url);
        Assert.True(result.IsAllowed, $"Expected allowed for: {url}, but got: {result.BlockReason}");
    }

    [Theory]
    [InlineData("https://twitter.com/groups/test",       "allowlist")]
    [InlineData("https://reddit.com/r/test",             "allowlist")]
    [InlineData("https://www.instagram.com/p/test",      "allowlist")]
    [InlineData("https://unknown-domain.com/community",  "allowlist")]
    public void CheckNavigation_BlocksNonAllowlistedDomains(string url, string expectedReason)
    {
        var gate   = BuildGate();
        var result = gate.CheckNavigation(url);
        Assert.False(result.IsAllowed);
        Assert.Contains(expectedReason, result.BlockReason ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("https://www.facebook.com/login")]
    [InlineData("https://www.facebook.com/checkpoint/abc")]
    [InlineData("https://m.facebook.com/messages")]
    public void CheckNavigation_BlocksLoginAndPrivatePaths(string url)
    {
        var gate   = BuildGate();
        var result = gate.CheckNavigation(url);
        Assert.False(result.IsAllowed);
    }

    [Fact]
    public void CheckNavigation_BlocksWhenSafeLockActive()
    {
        var gate   = BuildGate(isLocked: true);
        var result = gate.CheckNavigation("https://www.facebook.com/groups/public");
        Assert.False(result.IsAllowed);
        Assert.Contains("SAFE_LOCK", result.BlockReason ?? "");
    }

    [Fact]
    public void CheckPageState_BlocksWhenLoginRequired()
    {
        var gate   = BuildGate();
        var result = gate.CheckPageState(isPageAvailable: true, isLoginRequired: true, currentUrl: "https://www.facebook.com/login");
        Assert.False(result.IsAllowed);
        Assert.Contains("login", result.BlockReason ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CheckPageState_BlocksUnavailablePage()
    {
        var gate   = BuildGate();
        var result = gate.CheckPageState(isPageAvailable: false, isLoginRequired: false, currentUrl: "https://www.facebook.com/groups/test");
        Assert.False(result.IsAllowed);
    }

    [Fact]
    public void CheckPageState_AllowsAvailablePublicPage()
    {
        var gate   = BuildGate();
        var result = gate.CheckPageState(isPageAvailable: true, isLoginRequired: false, currentUrl: "https://www.facebook.com/groups/public");
        Assert.True(result.IsAllowed);
    }
}

// ══════════════════════════════════════════════════════════════════════════════
// CommunityDiscoveryEngine Tests
// ══════════════════════════════════════════════════════════════════════════════

public class CommunityDiscoveryEngineTests
{
    [Theory]
    [InlineData("https://www.facebook.com/groups/example",    CommunityType.FacebookGroup)]
    [InlineData("https://www.facebook.com/marketplace/item",  CommunityType.MarketplacePage)]
    [InlineData("https://discord.com/channels/123/456",       CommunityType.DiscordChannel)]
    [InlineData("https://openchat.line.me/discover",          CommunityType.LINEOpenChat)]
    [InlineData("https://example.com/community",              CommunityType.Other)]
    public void DetectTypeFromUrl_ReturnsCorrectType(string url, CommunityType expected)
    {
        var type = CommunityDiscoveryEngine.DetectTypeFromUrl(url);
        Assert.Equal(expected, type);
    }

    [Fact]
    public async Task DiscoverAsync_ReturnsNull_WhenSafetyGateBlocks()
    {
        var executor = new Mock<IPlaywrightOperatorExecutor>();
        var gate     = new CommunitySafetyGate(new StaticSafeLockProvider(isLocked: true));
        var engine   = new CommunityDiscoveryEngine(executor.Object, gate);

        var result = await engine.DiscoverAsync("https://www.facebook.com/groups/test");

        Assert.Null(result);
        executor.Verify(e => e.NavigateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DiscoverAsync_ReturnsDiscoveryResult_ForPublicPage()
    {
        var executor = new Mock<IPlaywrightOperatorExecutor>();
        executor.Setup(e => e.NavigateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        executor.Setup(e => e.IsPageAvailableAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
        executor.Setup(e => e.IsLoginRequiredAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
        executor.Setup(e => e.GetCurrentUrlAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync("https://www.facebook.com/groups/publicgroup");
        executor.Setup(e => e.GetTextContentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("Public");
        executor.Setup(e => e.GetAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenerateFakePosts(25));

        var gate   = new CommunitySafetyGate(new StaticSafeLockProvider(false));
        var engine = new CommunityDiscoveryEngine(executor.Object, gate);

        var result = await engine.DiscoverAsync("https://www.facebook.com/groups/publicgroup");

        Assert.NotNull(result);
        Assert.Equal(CommunityType.FacebookGroup, result.Type);
        Assert.Equal(CommunityVisibility.Public, result.Visibility);
        Assert.False(result.RequiresLogin);
        Assert.Equal(ActivityLevel.High, result.ActivityLevel);
    }

    [Fact]
    public async Task DiscoverAsync_ReturnsLoginRequired_WhenPageGated()
    {
        var executor = new Mock<IPlaywrightOperatorExecutor>();
        executor.Setup(e => e.NavigateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        executor.Setup(e => e.IsPageAvailableAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
        executor.Setup(e => e.IsLoginRequiredAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
        executor.Setup(e => e.GetCurrentUrlAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync("https://www.facebook.com/login");

        var gate   = new CommunitySafetyGate(new StaticSafeLockProvider(false));
        var engine = new CommunityDiscoveryEngine(executor.Object, gate);

        var result = await engine.DiscoverAsync("https://www.facebook.com/groups/private");

        Assert.NotNull(result);
        Assert.True(result.RequiresLogin);
        Assert.Equal(CommunityVisibility.Private, result.Visibility);
    }

    private static IReadOnlyList<string> GenerateFakePosts(int count)
        => Enumerable.Range(1, count).Select(i => $"Post content {i}").ToList();
}

// ══════════════════════════════════════════════════════════════════════════════
// CommunityContextExtractor Tests
// ══════════════════════════════════════════════════════════════════════════════

public class CommunityContextExtractorTests
{
    private static CommunityContextExtractor BuildExtractor() => new();

    private static CommunityMetadata BuildMetadata(string url = "https://www.facebook.com/groups/test") =>
        new()
        {
            Name           = "Test Community",
            Url            = url,
            Type           = CommunityType.FacebookGroup,
            Visibility     = CommunityVisibility.Public,
            ContentDensity = 10
        };

    [Fact]
    public void Extract_ReturnsCommunityContext_WithCorrectType()
    {
        var metadata = BuildMetadata();
        var context  = BuildExtractor().Extract(metadata, []);

        Assert.Equal("https://www.facebook.com/groups/test", context.CommunityUrl);
        Assert.Equal(nameof(CommunityType.FacebookGroup), context.CommunityType);
    }

    [Fact]
    public void Extract_DetectsPainPoints_FromPostContent()
    {
        var posts = new[]
        {
            new CommunityPost { PostId = "P1", Content = "This is a real problem with the app",       PublishedAt = DateTime.UtcNow },
            new CommunityPost { PostId = "P2", Content = "The service is broken and not working",     PublishedAt = DateTime.UtcNow },
            new CommunityPost { PostId = "P3", Content = "I am really frustrated with the errors",    PublishedAt = DateTime.UtcNow }
        };

        var context = BuildExtractor().Extract(BuildMetadata(), posts);

        Assert.NotEmpty(context.PainPoints);
    }

    [Fact]
    public void Extract_DetectsUrgencySignals_FromPostContent()
    {
        var posts = new[]
        {
            new CommunityPost { PostId = "P1", Content = "URGENT: please help me immediately!", PublishedAt = DateTime.UtcNow },
            new CommunityPost { PostId = "P2", Content = "This is critical, deadline tomorrow", PublishedAt = DateTime.UtcNow }
        };

        var context = BuildExtractor().Extract(BuildMetadata(), posts);

        Assert.NotEmpty(context.UrgencySignals);
    }

    [Fact]
    public void Extract_DetectsRecurringIssues_WhenKeywordsAppearMultipleTimes()
    {
        var posts = new[]
        {
            new CommunityPost { PostId = "P1", Content = "This happens every time, it keeps breaking again", PublishedAt = DateTime.UtcNow },
            new CommunityPost { PostId = "P2", Content = "Same problem again — keeps crashing repeatedly",   PublishedAt = DateTime.UtcNow },
            new CommunityPost { PostId = "P3", Content = "It always does this again and again",              PublishedAt = DateTime.UtcNow }
        };

        var context = BuildExtractor().Extract(BuildMetadata(), posts);

        Assert.NotEmpty(context.RecurringIssues);
    }

    [Fact]
    public void Extract_DetectsTrendingTopics_FromHighReactionPosts()
    {
        var posts = new[]
        {
            new CommunityPost { PostId = "P1", Content = "Big announcement — major product update released!",       PublishedAt = DateTime.UtcNow, ReactionCount = 150 },
            new CommunityPost { PostId = "P2", Content = "Community milestone: 10,000 members achieved!",          PublishedAt = DateTime.UtcNow, ReactionCount = 200 },
            new CommunityPost { PostId = "P3", Content = "No reactions here",                                       PublishedAt = DateTime.UtcNow, ReactionCount = 0   }
        };

        var context = BuildExtractor().Extract(BuildMetadata(), posts);

        Assert.NotEmpty(context.TrendingTopics);
    }

    [Fact]
    public void Extract_ReturnsDeterministicResults_GivenSameInput()
    {
        var posts = new[]
        {
            new CommunityPost { PostId = "P1", Content = "The product is broken and not working for me", PublishedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new CommunityPost { PostId = "P2", Content = "Urgent help needed asap please",               PublishedAt = new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc) }
        };

        var extractor = BuildExtractor();
        var ctx1 = extractor.Extract(BuildMetadata(), posts);
        var ctx2 = extractor.Extract(BuildMetadata(), posts);

        // Core structural properties must be identical
        Assert.Equal(ctx1.CommunityUrl,  ctx2.CommunityUrl);
        Assert.Equal(ctx1.CommunityType, ctx2.CommunityType);
        Assert.Equal(ctx1.ActivityLevel, ctx2.ActivityLevel);
        Assert.Equal(ctx1.PainPoints.Count,    ctx2.PainPoints.Count);
        Assert.Equal(ctx1.UrgencySignals.Count, ctx2.UrgencySignals.Count);
    }

    [Fact]
    public void Extract_ReturnsEmptySignals_WhenNoPosts()
    {
        var context = BuildExtractor().Extract(BuildMetadata(), []);
        Assert.Empty(context.PainPoints);
        Assert.Empty(context.UrgencySignals);
        Assert.Empty(context.RecurringIssues);
        Assert.Empty(context.TrendingTopics);
    }
}

// ══════════════════════════════════════════════════════════════════════════════
// CommunityConnector Tests
// ══════════════════════════════════════════════════════════════════════════════

public class CommunityConnectorTests
{
    private static (CommunityConnector connector, Mock<IPlaywrightOperatorExecutor> executor)
        BuildConnector(bool isLocked = false)
    {
        var executor = new Mock<IPlaywrightOperatorExecutor>();
        var gate     = new CommunitySafetyGate(new StaticSafeLockProvider(isLocked));
        var engine   = new CommunityDiscoveryEngine(executor.Object, gate);
        var connector = new CommunityConnector(executor.Object, gate, engine);
        return (connector, executor);
    }

    private static void SetupPublicPageMocks(Mock<IPlaywrightOperatorExecutor> executor, int postCount = 5)
    {
        executor.Setup(e => e.NavigateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        executor.Setup(e => e.IsPageAvailableAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
        executor.Setup(e => e.IsLoginRequiredAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
        executor.Setup(e => e.GetCurrentUrlAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync("https://www.facebook.com/groups/testgroup");
        executor.Setup(e => e.GetTextContentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("Test Community");
        executor.Setup(e => e.GetAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Enumerable.Range(1, postCount).Select(i => $"Post number {i}").ToList());
    }

    [Fact]
    public async Task ScanAsync_ReturnsSuccess_ForPublicFacebookGroup()
    {
        var (connector, executor) = BuildConnector();
        SetupPublicPageMocks(executor, postCount: 3);

        var request = new CommunityScanRequest
        {
            RequestId    = "REQ-001",
            CommunityUrl = "https://www.facebook.com/groups/testgroup",
            MaxPostCount = 10
        };

        var result = await connector.ScanAsync(request);

        Assert.Equal(ScanStatus.Success, result.Status);
        Assert.Equal("REQ-001", result.RequestId);
        Assert.NotNull(result.Metadata);
        Assert.True(result.ElapsedMs >= 0);
    }

    [Fact]
    public async Task ScanAsync_BlockedBySafeLock_ReturnsSafeLockStatus()
    {
        var (connector, executor) = BuildConnector(isLocked: true);

        var request = new CommunityScanRequest
        {
            RequestId    = "REQ-002",
            CommunityUrl = "https://www.facebook.com/groups/testgroup"
        };

        var result = await connector.ScanAsync(request);

        Assert.Equal(ScanStatus.BlockedBySafeLock, result.Status);
        Assert.Contains("SAFE_LOCK", result.BlockReason ?? "");
        // No navigation should occur when SAFE_LOCK is active
        executor.Verify(e => e.NavigateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ScanAsync_BlockedByAllowlist_ForUnknownDomain()
    {
        var (connector, _) = BuildConnector();

        var request = new CommunityScanRequest
        {
            RequestId    = "REQ-003",
            CommunityUrl = "https://twitter.com/groups/test"
        };

        var result = await connector.ScanAsync(request);

        Assert.Equal(ScanStatus.BlockedByAllowlist, result.Status);
    }

    [Fact]
    public async Task ScanAsync_BlockedLoginRequired_WhenPageGated()
    {
        var (connector, executor) = BuildConnector();

        executor.Setup(e => e.NavigateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        executor.Setup(e => e.IsPageAvailableAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
        executor.Setup(e => e.IsLoginRequiredAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
        executor.Setup(e => e.GetCurrentUrlAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync("https://www.facebook.com/login");
        executor.Setup(e => e.GetAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<string>());

        var request = new CommunityScanRequest
        {
            RequestId    = "REQ-004",
            CommunityUrl = "https://www.facebook.com/groups/private"
        };

        var result = await connector.ScanAsync(request);

        Assert.Equal(ScanStatus.BlockedLoginRequired, result.Status);
    }

    [Fact]
    public async Task ScanAsync_IsDeterministic_GivenSameInput()
    {
        var (connector, executor) = BuildConnector();
        SetupPublicPageMocks(executor, postCount: 5);

        var request = new CommunityScanRequest
        {
            RequestId    = "REQ-DET",
            CommunityUrl = "https://www.facebook.com/groups/testgroup",
            MaxPostCount = 5
        };

        var result1 = await connector.ScanAsync(request);
        var result2 = await connector.ScanAsync(request);

        Assert.Equal(result1.Status,               result2.Status);
        Assert.Equal(result1.RequestId,             result2.RequestId);
        Assert.Equal(result1.Metadata?.Name,        result2.Metadata?.Name);
        Assert.Equal(result1.Metadata?.PostCount,   result2.Metadata?.PostCount);
        Assert.Equal(result1.Metadata?.CommunityType, result2.Metadata?.CommunityType);
    }
}

// ══════════════════════════════════════════════════════════════════════════════
// DirectiveExecutionService Tests
// ══════════════════════════════════════════════════════════════════════════════

public class DirectiveExecutionServiceTests
{
    private static DirectiveExecutionService BuildService(bool isLocked = false, int postCount = 3)
    {
        var executor = new Mock<IPlaywrightOperatorExecutor>();
        executor.Setup(e => e.NavigateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        executor.Setup(e => e.IsPageAvailableAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
        executor.Setup(e => e.IsLoginRequiredAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
        executor.Setup(e => e.GetCurrentUrlAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync("https://www.facebook.com/groups/testgroup");
        executor.Setup(e => e.GetTextContentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("Test Group");
        executor.Setup(e => e.GetAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Enumerable.Range(1, postCount).Select(i => $"Post {i}").ToList());

        var safeLockProvider = new StaticSafeLockProvider(isLocked);
        var gate             = new CommunitySafetyGate(safeLockProvider);
        var engine           = new CommunityDiscoveryEngine(executor.Object, gate);
        var connector        = new CommunityConnector(executor.Object, gate, engine);
        var extractor        = new CommunityContextExtractor();

        return new DirectiveExecutionService(safeLockProvider, connector, extractor, gate);
    }

    [Fact]
    public async Task ExecuteAsync_ScanCommunity_ReturnsSuccess()
    {
        var service = BuildService();

        var directive = new OperatorDirective
        {
            DirectiveId  = "DIR-001",
            ActionType   = OperatorActionType.ScanCommunity,
            CommunityUrl = "https://www.facebook.com/groups/testgroup"
        };

        var result = await service.ExecuteAsync(directive);

        Assert.True(result.Success);
        Assert.Equal("DIR-001", result.DirectiveId);
        Assert.NotNull(result.ScanResult);
        Assert.Equal(ScanStatus.Success, result.ScanResult!.Status);
    }

    [Fact]
    public async Task ExecuteAsync_BlocksAllActions_WhenSafeLockActive()
    {
        var service = BuildService(isLocked: true);

        var directive = new OperatorDirective
        {
            DirectiveId  = "DIR-002",
            ActionType   = OperatorActionType.ScanCommunity,
            CommunityUrl = "https://www.facebook.com/groups/testgroup"
        };

        var result = await service.ExecuteAsync(directive);

        Assert.False(result.Success);
        Assert.Contains("SAFE_LOCK", result.Error ?? "");
    }

    [Fact]
    public async Task ExecuteAsync_ExtractCommunityContext_ReturnsContext()
    {
        // Pre-populate a scan result so ExtractCommunityContext doesn't need to scan again
        var scanResult = new CommunityScanResult
        {
            RequestId    = "DIR-003",
            Status       = ScanStatus.Success,
            CommunityUrl = "https://www.facebook.com/groups/testgroup",
            Metadata     = new CommunityMetadataSnapshot
            {
                Name          = "Test Group",
                CommunityType = nameof(CommunityType.FacebookGroup),
                Visibility    = nameof(CommunityVisibility.Public),
                PostCount     = 5
            }
        };

        var service = BuildService();

        var directive = new OperatorDirective
        {
            DirectiveId  = "DIR-003",
            ActionType   = OperatorActionType.ExtractCommunityContext,
            ScanResult   = scanResult
        };

        var result = await service.ExecuteAsync(directive);

        Assert.True(result.Success);
        Assert.NotNull(result.CommunityContext);
        Assert.Equal("https://www.facebook.com/groups/testgroup", result.CommunityContext!.CommunityUrl);
    }

    [Fact]
    public async Task ExecuteAsync_ScanCommunity_BlockedByAllowlist()
    {
        var service = BuildService();

        var directive = new OperatorDirective
        {
            DirectiveId  = "DIR-004",
            ActionType   = OperatorActionType.ScanCommunity,
            CommunityUrl = "https://twitter.com/groups/test"
        };

        var result = await service.ExecuteAsync(directive);

        Assert.False(result.Success);
        Assert.Contains("Policy gate blocked", result.Error ?? "");
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsError_ForMissingCommunityUrl()
    {
        var service = BuildService();

        var directive = new OperatorDirective
        {
            DirectiveId = "DIR-005",
            ActionType  = OperatorActionType.ScanCommunity,
            // CommunityUrl intentionally omitted
        };

        var result = await service.ExecuteAsync(directive);

        Assert.False(result.Success);
        Assert.Contains("CommunityUrl is required", result.Error ?? "");
    }
}
