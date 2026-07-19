// ══════════════════════════════════════════════════════════════════════════════
// META7 Operator — Community Connector Layer
// CommunitySafetyGate: Enforces domain allowlist, SAFE_LOCK, and read-only mode
// ══════════════════════════════════════════════════════════════════════════════

namespace META7.Operator.Api.Community;

/// <summary>
/// Result of a safety gate check.
/// </summary>
public sealed record SafetyGateResult(bool IsAllowed, string? BlockReason);

/// <summary>
/// Interface for SAFE_LOCK state provider.
/// Allows the gate to query lock status without coupling to any specific implementation.
/// </summary>
public interface ISafeLockProvider
{
    /// <summary>Returns true when SAFE_LOCK is active and all non-read-only operations are blocked.</summary>
    bool IsSafeLockActive { get; }
}

/// <summary>
/// Enforces three safety invariants before any community navigation is allowed:
/// 1. Domain allowlist — URL must match an approved community host
/// 2. SAFE_LOCK — no operations when the operator is in safe-lock state
/// 3. Private / login-required pages — navigation to gated pages is blocked
/// </summary>
public sealed class CommunitySafetyGate
{
    private readonly ISafeLockProvider _safeLock;

    // Approved public community domains — no login-required, no private pages
    private static readonly HashSet<string> AllowedHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "www.facebook.com",
        "facebook.com",
        "m.facebook.com",
        "discord.com",
        "discordapp.com",
        "line.me",
        "chat.line.me",
        "openchat.line.me",
        "marketplace.meta.com",
        "web.facebook.com"
    };

    // URL path patterns that are known to require login or indicate private pages
    private static readonly string[] BlockedPathPatterns =
    [
        "/login",
        "/checkpoint",
        "/privacy",
        "/recover",
        "/messages",
        "/notifications"
    ];

    public CommunitySafetyGate(ISafeLockProvider safeLock)
    {
        _safeLock = safeLock ?? throw new ArgumentNullException(nameof(safeLock));
    }

    /// <summary>
    /// Checks whether navigation to <paramref name="url"/> is permitted.
    /// Returns a <see cref="SafetyGateResult"/> with a human-readable block reason on failure.
    /// </summary>
    public SafetyGateResult CheckNavigation(string url)
    {
        if (_safeLock.IsSafeLockActive)
            return new SafetyGateResult(false, "SAFE_LOCK is active — all community navigation blocked");

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return new SafetyGateResult(false, $"Invalid URL format: {url}");

        if (uri.Scheme is not "https" and not "http")
            return new SafetyGateResult(false, $"Non-HTTP scheme not allowed: {uri.Scheme}");

        if (!AllowedHosts.Contains(uri.Host))
            return new SafetyGateResult(false, $"Domain not in allowlist: {uri.Host}");

        var pathLower = uri.AbsolutePath.ToLowerInvariant();
        foreach (var blocked in BlockedPathPatterns)
        {
            if (pathLower.StartsWith(blocked, StringComparison.Ordinal))
                return new SafetyGateResult(false, $"Blocked path pattern '{blocked}' detected in URL");
        }

        return new SafetyGateResult(true, null);
    }

    /// <summary>
    /// Checks a post-navigation page state to ensure no login wall was encountered.
    /// </summary>
    public SafetyGateResult CheckPageState(bool isPageAvailable, bool isLoginRequired, string currentUrl)
    {
        if (_safeLock.IsSafeLockActive)
            return new SafetyGateResult(false, "SAFE_LOCK is active");

        if (isLoginRequired)
            return new SafetyGateResult(false, "Page requires login — read-only access blocked");

        if (!isPageAvailable)
            return new SafetyGateResult(false, "Page unavailable or private");

        // Re-validate the final URL in case of redirect to a login page
        var urlCheck = CheckNavigation(currentUrl);
        if (!urlCheck.IsAllowed)
            return new SafetyGateResult(false, $"Redirect to blocked URL: {urlCheck.BlockReason}");

        return new SafetyGateResult(true, null);
    }

    /// <summary>Returns the set of allowed community hosts (for testing and documentation).</summary>
    public static IReadOnlySet<string> GetAllowedHosts() => AllowedHosts;
}
