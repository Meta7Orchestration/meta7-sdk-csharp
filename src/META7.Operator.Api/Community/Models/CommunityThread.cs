// ══════════════════════════════════════════════════════════════════════════════
// META7 Operator — Community Connector Layer
// CommunityThread: Nested reply thread model
// ══════════════════════════════════════════════════════════════════════════════

namespace META7.Operator.Api.Community.Models;

/// <summary>
/// A reply thread nested under a community post, extracted in read-only mode.
/// Depth is bounded by CommunityScanRequest.MaxThreadDepth.
/// </summary>
public sealed record CommunityThread
{
    /// <summary>Stable identifier for this thread node.</summary>
    public required string ThreadId { get; init; }

    /// <summary>Depth level (0 = direct reply, 1 = reply-to-reply, etc.).</summary>
    public required int Depth { get; init; }

    /// <summary>Text content of the reply.</summary>
    public required string Content { get; init; }

    /// <summary>UTC timestamp of the reply.</summary>
    public required DateTime PublishedAt { get; init; }

    /// <summary>Visible reaction count on this reply.</summary>
    public int ReactionCount { get; init; }

    /// <summary>Child replies (if depth < MaxThreadDepth).</summary>
    public IReadOnlyList<CommunityThread> Children { get; init; } = [];
}
