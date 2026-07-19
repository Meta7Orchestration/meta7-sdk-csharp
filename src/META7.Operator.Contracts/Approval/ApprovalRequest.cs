// ══════════════════════════════════════════════════════════════════════════════
// META7 Operator Contracts — Approval Request DTO
// Human Approval Layer: serializable package for every pending write action.
// ══════════════════════════════════════════════════════════════════════════════

namespace META7.Operator.Contracts.Approval;

/// <summary>
/// Immutable snapshot of a write-capable action awaiting human approval.
/// Created by HumanApprovalGateway and stored in HumanApprovalQueue.
/// </summary>
public sealed record ApprovalRequest(
    /// <summary>Unique identifier for this approval request (GUID).</summary>
    string RequestId,

    /// <summary>ID of the source directive that triggered this action.</summary>
    string DirectiveId,

    /// <summary>The write-capable action type that requires human approval.</summary>
    OperatorActionType ActionType,

    /// <summary>
    /// Serialized action payload. Content is determined by ActionType.
    /// Must not contain credentials, PII, or sensitive data.
    /// </summary>
    string Payload,

    /// <summary>UTC timestamp when this request was created.</summary>
    DateTime CreatedAt,

    /// <summary>Optional originating domain for allowlist enforcement.</summary>
    string? Domain = null
);
