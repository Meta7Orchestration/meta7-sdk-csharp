// ══════════════════════════════════════════════════════════════════════════════
// META7 Operator — Community Connector Layer
// CommunityDiscoveryEngine: Identifies community type, visibility, and activity
// No login, no join actions
// ══════════════════════════════════════════════════════════════════════════════

using META7.Operator.Api.Community.Models;
using META7.Operator.Contracts.Community;

namespace META7.Operator.Api.Community;

/// <summary>
/// Discovery result for a community URL.
/// </summary>
public sealed record CommunityDiscoveryResult
{
    public required CommunityType Type { get; init; }
    public required CommunityVisibility Visibility { get; init; }
    public required double ContentDensity { get; init; }
    public required ActivityLevel ActivityLevel { get; init; }
    public bool RequiresLogin { get; init; }
}

/// <summary>
/// Identifies the type, visibility, content density, and activity level of a community.
/// Uses only publicly available page signals — no login or join is attempted.
/// </summary>
public sealed class CommunityDiscoveryEngine
{
    private readonly IPlaywrightOperatorExecutor _executor;
    private readonly CommunitySafetyGate _safetyGate;

    public CommunityDiscoveryEngine(
        IPlaywrightOperatorExecutor executor,
        CommunitySafetyGate safetyGate)
    {
        _executor   = executor   ?? throw new ArgumentNullException(nameof(executor));
        _safetyGate = safetyGate ?? throw new ArgumentNullException(nameof(safetyGate));
    }

    /// <summary>
    /// Discovers community attributes from <paramref name="url"/> in read-only mode.
    /// Returns null when safety gate blocks navigation.
    /// </summary>
    public async Task<CommunityDiscoveryResult?> DiscoverAsync(string url, CancellationToken cancellationToken = default)
    {
        var gateCheck = _safetyGate.CheckNavigation(url);
        if (!gateCheck.IsAllowed)
            return null;

        var type = DetectTypeFromUrl(url);

        await _executor.NavigateAsync(url, cancellationToken);

        var isAvailable   = await _executor.IsPageAvailableAsync(cancellationToken);
        var loginRequired = await _executor.IsLoginRequiredAsync(cancellationToken);
        var currentUrl    = await _executor.GetCurrentUrlAsync(cancellationToken);

        var pageCheck = _safetyGate.CheckPageState(isAvailable, loginRequired, currentUrl);
        if (!pageCheck.IsAllowed)
            return new CommunityDiscoveryResult
            {
                Type           = type,
                Visibility     = loginRequired ? CommunityVisibility.Private : CommunityVisibility.Unknown,
                ContentDensity = 0,
                ActivityLevel  = ActivityLevel.Low,
                RequiresLogin  = loginRequired
            };

        var visibility     = await DetectVisibilityAsync(type, cancellationToken);
        var contentDensity = await EstimateContentDensityAsync(cancellationToken);
        var activityLevel  = ClassifyActivityLevel(contentDensity);

        return new CommunityDiscoveryResult
        {
            Type           = type,
            Visibility     = visibility,
            ContentDensity = contentDensity,
            ActivityLevel  = activityLevel,
            RequiresLogin  = false
        };
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    public static CommunityType DetectTypeFromUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return CommunityType.Other;

        var host = uri.Host.ToLowerInvariant();
        var path = uri.AbsolutePath.ToLowerInvariant();

        if (host.Contains("facebook") || host.Contains("meta.com"))
        {
            return path.Contains("/groups/") ? CommunityType.FacebookGroup
                 : path.Contains("/marketplace/") ? CommunityType.MarketplacePage
                 : CommunityType.FacebookGroup;
        }

        if (host.Contains("discord"))
            return CommunityType.DiscordChannel;

        if (host.Contains("line.me"))
            return CommunityType.LINEOpenChat;

        return CommunityType.Other;
    }

    private async Task<CommunityVisibility> DetectVisibilityAsync(CommunityType type, CancellationToken ct)
    {
        // Look for common public/private indicators in page text
        var visibilityText = await _executor.GetTextContentAsync("[data-testid='privacy-setting'], .group-visibility, .channel-type", ct);

        if (visibilityText is null)
            return CommunityVisibility.Unknown;

        var lower = visibilityText.ToLowerInvariant();

        if (lower.Contains("public"))
            return CommunityVisibility.Public;

        if (lower.Contains("private") || lower.Contains("closed") || lower.Contains("secret"))
            return CommunityVisibility.Private;

        if (lower.Contains("restricted") || lower.Contains("member"))
            return CommunityVisibility.Restricted;

        return CommunityVisibility.Unknown;
    }

    private async Task<double> EstimateContentDensityAsync(CancellationToken ct)
    {
        // Count visible post elements as a density proxy
        var posts = await _executor.GetAllTextAsync("[role='article'], .post, .message, .feed-item", ct);
        return posts.Count;
    }

    private static ActivityLevel ClassifyActivityLevel(double postCount) =>
        postCount switch
        {
            >= 50  => ActivityLevel.VeryHigh,
            >= 20  => ActivityLevel.High,
            >= 5   => ActivityLevel.Moderate,
            _      => ActivityLevel.Low
        };
}
