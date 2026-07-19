// ══════════════════════════════════════════════════════════════════════════════
// META7 Operator — Community Connector Layer
// CommunityScanRequest: Input contract for community scanning
// Read-Only · No write actions permitted
// ══════════════════════════════════════════════════════════════════════════════

namespace META7.Operator.Contracts.Community;

/// <summary>
/// Request to scan a community URL in read-only mode.
/// No authentication, no join, no write actions are performed.
/// </summary>
public sealed record CommunityScanRequest
{
    /// <summary>Unique request identifier for deterministic tracing.</summary>
    public required string RequestId { get; init; }

    /// <summary>Public URL of the community to scan (must pass domain allowlist).</summary>
    public required string CommunityUrl { get; init; }

    /// <summary>Maximum number of recent public posts to extract.</summary>
    public int MaxPostCount { get; init; } = 20;

    /// <summary>Maximum depth of thread replies to extract.</summary>
    public int MaxThreadDepth { get; init; } = 3;

    /// <summary>Whether to extract visible reaction counts (read-only).</summary>
    public bool IncludeReactions { get; init; } = true;

    /// <summary>UTC timestamp when the request was issued.</summary>
    public DateTime IssuedAt { get; init; } = DateTime.UtcNow;
}
