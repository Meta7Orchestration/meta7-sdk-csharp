// ══════════════════════════════════════════════════════════════════════════════
// META7 Operator — Community Connector Layer
// OperatorActionType: Read-only community action types
// ══════════════════════════════════════════════════════════════════════════════

namespace META7.Operator.Api;

/// <summary>
/// Operator action types available to the directive execution service.
/// Only read-only community actions are defined here; no write actions.
/// </summary>
public enum OperatorActionType
{
    /// <summary>
    /// Scan a public community page (read-only).
    /// Navigates to the community URL and extracts metadata, posts, and threads.
    /// </summary>
    ScanCommunity,

    /// <summary>
    /// Extract structured analytical context from a community scan result (read-only).
    /// Identifies pain points, recurring issues, trending topics, and urgency signals.
    /// </summary>
    ExtractCommunityContext
}
