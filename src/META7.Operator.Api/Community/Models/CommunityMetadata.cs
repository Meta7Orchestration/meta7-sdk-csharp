// ══════════════════════════════════════════════════════════════════════════════
// META7 Operator — Community Connector Layer
// CommunityMetadata: Full metadata model for a community page
// ══════════════════════════════════════════════════════════════════════════════

namespace META7.Operator.Api.Community.Models;

/// <summary>Type of community platform.</summary>
public enum CommunityType
{
    FacebookGroup,
    LINEOpenChat,
    DiscordChannel,
    MarketplacePage,
    Other
}

/// <summary>Visibility classification of a community.</summary>
public enum CommunityVisibility
{
    Public,
    Restricted,
    Private,
    Unknown
}

/// <summary>
/// Full metadata extracted from a community page in read-only mode.
/// </summary>
public sealed record CommunityMetadata
{
    /// <summary>Display name of the community.</summary>
    public required string Name { get; init; }

    /// <summary>Community URL that was scanned.</summary>
    public required string Url { get; init; }

    /// <summary>Detected community platform type.</summary>
    public required CommunityType Type { get; init; }

    /// <summary>Visibility classification.</summary>
    public required CommunityVisibility Visibility { get; init; }

    /// <summary>Public member count (null if not visible without login).</summary>
    public int? MemberCount { get; init; }

    /// <summary>Short description from the community about page (if public).</summary>
    public string? Description { get; init; }

    /// <summary>Date the community was created (if visible).</summary>
    public DateTime? CreatedAt { get; init; }

    /// <summary>Content density score: posts per day estimate.</summary>
    public double ContentDensity { get; init; }

    /// <summary>UTC timestamp of metadata extraction.</summary>
    public DateTime ExtractedAt { get; init; } = DateTime.UtcNow;
}
