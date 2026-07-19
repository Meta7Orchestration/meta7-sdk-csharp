// ══════════════════════════════════════════════════════════════════════════════
// META7 Operator — Community Connector Layer
// CommunityPost: Model for a single public post
// ══════════════════════════════════════════════════════════════════════════════

namespace META7.Operator.Api.Community.Models;

/// <summary>
/// A single public post extracted from a community page in read-only mode.
/// Contains only publicly visible, anonymised content — no PII is stored.
/// </summary>
public sealed record CommunityPost
{
    /// <summary>Stable identifier derived from post position and timestamp hash.</summary>
    public required string PostId { get; init; }

    /// <summary>Text content of the post (truncated to 2000 chars max).</summary>
    public required string Content { get; init; }

    /// <summary>UTC timestamp when the post was published.</summary>
    public required DateTime PublishedAt { get; init; }

    /// <summary>Visible reaction count (read-only, aggregated — no user attribution).</summary>
    public int ReactionCount { get; init; }

    /// <summary>Number of replies/comments on the post.</summary>
    public int ReplyCount { get; init; }

    /// <summary>Nested reply threads (up to configured max depth).</summary>
    public IReadOnlyList<CommunityThread> Threads { get; init; } = [];
}
