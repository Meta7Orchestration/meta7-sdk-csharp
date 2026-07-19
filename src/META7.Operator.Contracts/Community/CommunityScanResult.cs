// ══════════════════════════════════════════════════════════════════════════════
// META7 Operator — Community Connector Layer
// CommunityScanResult: Output contract for community scanning
// Read-Only · No write actions permitted
// ══════════════════════════════════════════════════════════════════════════════

namespace META7.Operator.Contracts.Community;

/// <summary>Status of a community scan operation.</summary>
public enum ScanStatus
{
    Success,
    BlockedByAllowlist,
    BlockedBySafeLock,
    BlockedLoginRequired,
    BlockedPrivatePage,
    Failed
}

/// <summary>
/// Result of a read-only community scan.
/// Contains deterministic, structured data extracted from a public community page.
/// </summary>
public sealed record CommunityScanResult
{
    /// <summary>Echoed request identifier for tracing.</summary>
    public required string RequestId { get; init; }

    /// <summary>Status of the scan operation.</summary>
    public required ScanStatus Status { get; init; }

    /// <summary>Scanned community URL.</summary>
    public required string CommunityUrl { get; init; }

    /// <summary>Extracted community metadata (null when status is not Success).</summary>
    public CommunityMetadataSnapshot? Metadata { get; init; }

    /// <summary>Human-readable reason when scan was blocked or failed.</summary>
    public string? BlockReason { get; init; }

    /// <summary>UTC timestamp when scan completed.</summary>
    public DateTime ScannedAt { get; init; } = DateTime.UtcNow;

    /// <summary>Elapsed milliseconds for the scan operation.</summary>
    public long ElapsedMs { get; init; }
}

/// <summary>Lightweight metadata snapshot embedded in scan results.</summary>
public sealed record CommunityMetadataSnapshot
{
    public required string Name { get; init; }
    public required string CommunityType { get; init; }
    public required string Visibility { get; init; }
    public int? MemberCount { get; init; }
    public int PostCount { get; init; }
    public string? Description { get; init; }
}
