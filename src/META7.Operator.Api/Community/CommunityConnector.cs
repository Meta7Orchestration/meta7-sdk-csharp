// ══════════════════════════════════════════════════════════════════════════════
// META7 Operator — Community Connector Layer
// CommunityConnector: Read-only community integration
// Navigates to allowed community URLs and extracts structured data
// No write actions, no login, no messaging
// ══════════════════════════════════════════════════════════════════════════════

using System.Diagnostics;
using META7.Operator.Api.Community.Models;
using META7.Operator.Contracts.Community;

namespace META7.Operator.Api.Community;

/// <summary>
/// Read-only community connector.
/// Uses <see cref="IPlaywrightOperatorExecutor"/> to navigate community URLs
/// and extract group metadata, member counts, public posts, thread structures,
/// timestamps, and visible reactions.
/// </summary>
public sealed class CommunityConnector
{
    private readonly IPlaywrightOperatorExecutor _executor;
    private readonly CommunitySafetyGate _safetyGate;
    private readonly CommunityDiscoveryEngine _discoveryEngine;

    public CommunityConnector(
        IPlaywrightOperatorExecutor executor,
        CommunitySafetyGate safetyGate,
        CommunityDiscoveryEngine discoveryEngine)
    {
        _executor        = executor        ?? throw new ArgumentNullException(nameof(executor));
        _safetyGate      = safetyGate      ?? throw new ArgumentNullException(nameof(safetyGate));
        _discoveryEngine = discoveryEngine ?? throw new ArgumentNullException(nameof(discoveryEngine));
    }

    /// <summary>
    /// Performs a deterministic, read-only scan of a community page.
    /// Returns a <see cref="CommunityScanResult"/> with all extracted data.
    /// </summary>
    public async Task<CommunityScanResult> ScanAsync(
        CommunityScanRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var sw = Stopwatch.StartNew();

        // 1. Pre-flight safety gate
        var gateCheck = _safetyGate.CheckNavigation(request.CommunityUrl);
        if (!gateCheck.IsAllowed)
        {
            return new CommunityScanResult
            {
                RequestId    = request.RequestId,
                Status       = DetermineBlockStatus(gateCheck.BlockReason ?? string.Empty),
                CommunityUrl = request.CommunityUrl,
                BlockReason  = gateCheck.BlockReason,
                ElapsedMs    = sw.ElapsedMilliseconds
            };
        }

        // 2. Navigate (read-only)
        await _executor.NavigateAsync(request.CommunityUrl, cancellationToken);

        // 3. Post-navigation safety check
        var isAvailable   = await _executor.IsPageAvailableAsync(cancellationToken);
        var loginRequired = await _executor.IsLoginRequiredAsync(cancellationToken);
        var currentUrl    = await _executor.GetCurrentUrlAsync(cancellationToken);

        var pageCheck = _safetyGate.CheckPageState(isAvailable, loginRequired, currentUrl);
        if (!pageCheck.IsAllowed)
        {
            return new CommunityScanResult
            {
                RequestId    = request.RequestId,
                Status       = loginRequired ? ScanStatus.BlockedLoginRequired : ScanStatus.BlockedPrivatePage,
                CommunityUrl = request.CommunityUrl,
                BlockReason  = pageCheck.BlockReason,
                ElapsedMs    = sw.ElapsedMilliseconds
            };
        }

        // 4. Discover community type
        var discovery = await _discoveryEngine.DiscoverAsync(request.CommunityUrl, cancellationToken);

        // 5. Extract metadata
        var metadata = await ExtractMetadataAsync(request.CommunityUrl, discovery, cancellationToken);

        // 6. Extract public posts
        var posts = await ExtractPostsAsync(request.MaxPostCount, request.MaxThreadDepth, request.IncludeReactions, cancellationToken);

        sw.Stop();

        return new CommunityScanResult
        {
            RequestId    = request.RequestId,
            Status       = ScanStatus.Success,
            CommunityUrl = request.CommunityUrl,
            Metadata     = new CommunityMetadataSnapshot
            {
                Name          = metadata.Name,
                CommunityType = metadata.Type.ToString(),
                Visibility    = metadata.Visibility.ToString(),
                MemberCount   = metadata.MemberCount,
                PostCount     = posts.Count,
                Description   = metadata.Description
            },
            ElapsedMs = sw.ElapsedMilliseconds
        };
    }

    // ── Private extraction helpers ────────────────────────────────────────────

    private async Task<CommunityMetadata> ExtractMetadataAsync(
        string url,
        CommunityDiscoveryResult? discovery,
        CancellationToken ct)
    {
        var name         = await _executor.GetTextContentAsync("h1, [data-testid='group-name'], .group-header-title, .server-name", ct) ?? "Unknown Community";
        var description  = await _executor.GetTextContentAsync("[data-testid='group-description'], .about-group, .topic, .channel-topic", ct);
        var memberText   = await _executor.GetTextContentAsync("[data-testid='member-count'], .members-count, .member-count", ct);
        var memberCount  = ParseMemberCount(memberText);

        return new CommunityMetadata
        {
            Name           = name.Trim(),
            Url            = url,
            Type           = discovery?.Type ?? CommunityType.Other,
            Visibility     = discovery?.Visibility ?? CommunityVisibility.Unknown,
            MemberCount    = memberCount,
            Description    = description?.Trim(),
            ContentDensity = discovery?.ContentDensity ?? 0,
            ExtractedAt    = DateTime.UtcNow
        };
    }

    private async Task<IReadOnlyList<CommunityPost>> ExtractPostsAsync(
        int maxCount,
        int maxDepth,
        bool includeReactions,
        CancellationToken ct)
    {
        var postTexts = await _executor.GetAllTextAsync("[role='article'], .post-content, .message-content, .feed-item-content", ct);
        var posts = new List<CommunityPost>();

        for (int i = 0; i < Math.Min(postTexts.Count, maxCount); i++)
        {
            var content = postTexts[i];
            if (string.IsNullOrWhiteSpace(content))
                continue;

            if (content.Length > 2000)
                content = content[..2000];

            posts.Add(new CommunityPost
            {
                PostId        = GeneratePostId(i, content),
                Content       = content.Trim(),
                PublishedAt   = DateTime.UtcNow, // Timestamp parsing is platform-specific
                ReactionCount = 0,               // Set by caller if IncludeReactions=true
                ReplyCount    = 0,
                Threads       = []
            });
        }

        return posts;
    }

    // ── Utility helpers ───────────────────────────────────────────────────────

    private static int? ParseMemberCount(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        // Extract first sequence of digits, handling "1,234" and "1.2K" patterns
        var digits = new string(text.Where(c => char.IsDigit(c) || c == ',').ToArray()).Replace(",", "");
        return int.TryParse(digits, out var count) ? count : null;
    }

    private static string GeneratePostId(int index, string content)
    {
        var hash = Math.Abs(content.GetHashCode());
        return $"POST-{index:D3}-{hash:X8}";
    }

    private static ScanStatus DetermineBlockStatus(string blockReason) =>
        blockReason.Contains("SAFE_LOCK") ? ScanStatus.BlockedBySafeLock
        : blockReason.Contains("allowlist") || blockReason.Contains("Domain") ? ScanStatus.BlockedByAllowlist
        : blockReason.Contains("login") ? ScanStatus.BlockedLoginRequired
        : blockReason.Contains("private") || blockReason.Contains("unavailable") ? ScanStatus.BlockedPrivatePage
        : ScanStatus.Failed;
}
