// ══════════════════════════════════════════════════════════════════════════════
// META7 Operator — Community Connector Layer
// DirectiveExecutionService: Routes community actions to CommunityConnector
// Enforces SAFE_LOCK and policy gate before execution
// ══════════════════════════════════════════════════════════════════════════════

using META7.Operator.Api.Community;
using META7.Operator.Contracts.Community;

namespace META7.Operator.Api;

/// <summary>
/// Input directive for operator actions.
/// </summary>
public sealed record OperatorDirective
{
    /// <summary>Unique identifier for tracing.</summary>
    public required string DirectiveId { get; init; }

    /// <summary>Action type to execute.</summary>
    public required OperatorActionType ActionType { get; init; }

    /// <summary>Community URL (required for community actions).</summary>
    public string? CommunityUrl { get; init; }

    /// <summary>Max posts to extract (community scanning only).</summary>
    public int MaxPostCount { get; init; } = 20;

    /// <summary>Max thread depth to extract (community scanning only).</summary>
    public int MaxThreadDepth { get; init; } = 3;

    /// <summary>Whether to include reaction counts.</summary>
    public bool IncludeReactions { get; init; } = true;

    /// <summary>Pre-loaded scan result for ExtractCommunityContext action.</summary>
    public CommunityScanResult? ScanResult { get; init; }
}

/// <summary>Result of a directive execution.</summary>
public sealed record DirectiveExecutionResult
{
    public required string DirectiveId { get; init; }
    public required bool Success { get; init; }
    public string? Error { get; init; }
    public CommunityScanResult? ScanResult { get; init; }
    public CommunityContext? CommunityContext { get; init; }
}

/// <summary>
/// Routes operator directives to the appropriate service.
/// Enforces SAFE_LOCK and policy gate before dispatching any action.
/// </summary>
public sealed class DirectiveExecutionService
{
    private readonly ISafeLockProvider _safeLock;
    private readonly Community.CommunityConnector _communityConnector;
    private readonly Community.CommunityContextExtractor _contextExtractor;
    private readonly Community.CommunitySafetyGate _safetyGate;

    public DirectiveExecutionService(
        ISafeLockProvider safeLock,
        Community.CommunityConnector communityConnector,
        Community.CommunityContextExtractor contextExtractor,
        Community.CommunitySafetyGate safetyGate)
    {
        _safeLock           = safeLock           ?? throw new ArgumentNullException(nameof(safeLock));
        _communityConnector = communityConnector ?? throw new ArgumentNullException(nameof(communityConnector));
        _contextExtractor   = contextExtractor   ?? throw new ArgumentNullException(nameof(contextExtractor));
        _safetyGate         = safetyGate         ?? throw new ArgumentNullException(nameof(safetyGate));
    }

    /// <summary>
    /// Executes a directive.
    /// Returns an error result when SAFE_LOCK is active or the action is not permitted.
    /// </summary>
    public async Task<DirectiveExecutionResult> ExecuteAsync(
        OperatorDirective directive,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(directive);

        // Global SAFE_LOCK enforcement
        if (_safeLock.IsSafeLockActive)
        {
            return new DirectiveExecutionResult
            {
                DirectiveId = directive.DirectiveId,
                Success     = false,
                Error       = "SAFE_LOCK is active — directive execution blocked"
            };
        }

        return directive.ActionType switch
        {
            OperatorActionType.ScanCommunity        => await ExecuteScanAsync(directive, cancellationToken),
            OperatorActionType.ExtractCommunityContext => await ExecuteExtractContextAsync(directive, cancellationToken),
            _ => new DirectiveExecutionResult
            {
                DirectiveId = directive.DirectiveId,
                Success     = false,
                Error       = $"Unknown action type: {directive.ActionType}"
            }
        };
    }

    // ── Private action handlers ───────────────────────────────────────────────

    private async Task<DirectiveExecutionResult> ExecuteScanAsync(
        OperatorDirective directive,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(directive.CommunityUrl))
        {
            return new DirectiveExecutionResult
            {
                DirectiveId = directive.DirectiveId,
                Success     = false,
                Error       = "CommunityUrl is required for ScanCommunity action"
            };
        }

        // Policy gate: validate URL before dispatch
        var gateCheck = _safetyGate.CheckNavigation(directive.CommunityUrl);
        if (!gateCheck.IsAllowed)
        {
            return new DirectiveExecutionResult
            {
                DirectiveId = directive.DirectiveId,
                Success     = false,
                Error       = $"Policy gate blocked navigation: {gateCheck.BlockReason}"
            };
        }

        var request = new CommunityScanRequest
        {
            RequestId    = directive.DirectiveId,
            CommunityUrl = directive.CommunityUrl,
            MaxPostCount = directive.MaxPostCount,
            MaxThreadDepth = directive.MaxThreadDepth,
            IncludeReactions = directive.IncludeReactions
        };

        var result = await _communityConnector.ScanAsync(request, ct);

        return new DirectiveExecutionResult
        {
            DirectiveId = directive.DirectiveId,
            Success     = result.Status == ScanStatus.Success,
            Error       = result.Status == ScanStatus.Success ? null : result.BlockReason,
            ScanResult  = result
        };
    }

    private async Task<DirectiveExecutionResult> ExecuteExtractContextAsync(
        OperatorDirective directive,
        CancellationToken ct)
    {
        CommunityScanResult? scanResult = directive.ScanResult;

        // If no pre-loaded scan result, perform a scan first
        if (scanResult is null)
        {
            if (string.IsNullOrWhiteSpace(directive.CommunityUrl))
            {
                return new DirectiveExecutionResult
                {
                    DirectiveId = directive.DirectiveId,
                    Success     = false,
                    Error       = "CommunityUrl or ScanResult is required for ExtractCommunityContext"
                };
            }

            var scanDirective = directive with { ActionType = OperatorActionType.ScanCommunity };
            var scanExecResult = await ExecuteScanAsync(scanDirective, ct);
            if (!scanExecResult.Success)
                return scanExecResult with { DirectiveId = directive.DirectiveId };

            scanResult = scanExecResult.ScanResult;
        }

        if (scanResult?.Status != ScanStatus.Success || scanResult.Metadata is null)
        {
            return new DirectiveExecutionResult
            {
                DirectiveId = directive.DirectiveId,
                Success     = false,
                Error       = "Cannot extract context from a failed or incomplete scan"
            };
        }

        // Build lightweight metadata for the extractor
        var metadata = new Community.Models.CommunityMetadata
        {
            Name           = scanResult.Metadata.Name,
            Url            = scanResult.CommunityUrl,
            Type           = Enum.TryParse<Community.Models.CommunityType>(scanResult.Metadata.CommunityType, out var t) ? t : Community.Models.CommunityType.Other,
            Visibility     = Enum.TryParse<Community.Models.CommunityVisibility>(scanResult.Metadata.Visibility, out var v) ? v : Community.Models.CommunityVisibility.Unknown,
            MemberCount    = scanResult.Metadata.MemberCount,
            Description    = scanResult.Metadata.Description,
            ContentDensity = scanResult.Metadata.PostCount
        };

        var context = _contextExtractor.Extract(metadata, []);

        return new DirectiveExecutionResult
        {
            DirectiveId      = directive.DirectiveId,
            Success          = true,
            ScanResult       = scanResult,
            CommunityContext = context
        };
    }
}
