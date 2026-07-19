// ══════════════════════════════════════════════════════════════════════════════
// META7 Operator — Community Connector Layer
// CommunityContext: Structured analytical context extracted from community data
// Read-Only · No write actions permitted
// ══════════════════════════════════════════════════════════════════════════════

namespace META7.Operator.Contracts.Community;

/// <summary>Activity level classification of a community.</summary>
public enum ActivityLevel { Low, Moderate, High, VeryHigh }

/// <summary>
/// Structured analytical context derived from a community scan.
/// Identifies pain points, recurring issues, trends, and urgency signals.
/// Does NOT generate outreach messages.
/// </summary>
public sealed record CommunityContext
{
    /// <summary>Community URL this context was derived from.</summary>
    public required string CommunityUrl { get; init; }

    /// <summary>Type of community (FacebookGroup, LINEOpenChat, DiscordChannel, Other).</summary>
    public required string CommunityType { get; init; }

    /// <summary>Estimated activity level based on post frequency and reaction density.</summary>
    public required ActivityLevel ActivityLevel { get; init; }

    /// <summary>Recurring pain points identified in public posts.</summary>
    public IReadOnlyList<string> PainPoints { get; init; } = [];

    /// <summary>Issues that appear repeatedly across multiple posts or threads.</summary>
    public IReadOnlyList<string> RecurringIssues { get; init; } = [];

    /// <summary>Topics with high engagement or recent spike in posts.</summary>
    public IReadOnlyList<string> TrendingTopics { get; init; } = [];

    /// <summary>Signals indicating urgency (e.g., distress keywords, unanswered questions).</summary>
    public IReadOnlyList<string> UrgencySignals { get; init; } = [];

    /// <summary>UTC timestamp of context extraction.</summary>
    public DateTime ExtractedAt { get; init; } = DateTime.UtcNow;
}
