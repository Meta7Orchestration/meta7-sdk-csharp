// ══════════════════════════════════════════════════════════════════════════════
// META7 Operator — Community Connector Layer
// CommunitySignal: Analytical signal derived from community data
// ══════════════════════════════════════════════════════════════════════════════

namespace META7.Operator.Api.Community.Models;

/// <summary>Category of a community signal.</summary>
public enum SignalCategory
{
    PainPoint,
    RecurringIssue,
    TrendingTopic,
    UrgencySignal
}

/// <summary>
/// An analytical signal extracted from community content.
/// Represents a pattern identified across multiple posts or threads.
/// </summary>
public sealed record CommunitySignal
{
    /// <summary>Signal identifier.</summary>
    public required string SignalId { get; init; }

    /// <summary>Category of the signal.</summary>
    public required SignalCategory Category { get; init; }

    /// <summary>Human-readable label for the signal.</summary>
    public required string Label { get; init; }

    /// <summary>Number of posts/threads contributing to this signal.</summary>
    public required int Occurrences { get; init; }

    /// <summary>Confidence score (0.0 – 1.0).</summary>
    public required double Confidence { get; init; }

    /// <summary>Representative excerpt from source posts.</summary>
    public string? Excerpt { get; init; }

    /// <summary>UTC timestamp of signal extraction.</summary>
    public DateTime DetectedAt { get; init; } = DateTime.UtcNow;
}
