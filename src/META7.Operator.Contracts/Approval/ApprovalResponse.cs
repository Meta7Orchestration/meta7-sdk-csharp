// ══════════════════════════════════════════════════════════════════════════════
// META7 Operator Contracts — Approval Response DTO
// Human Approval Layer: current state of an approval request, returned by the
// HumanApprovalController to operators querying /v1/approvals/pending.
// ══════════════════════════════════════════════════════════════════════════════

namespace META7.Operator.Contracts.Approval;

/// <summary>
/// Read-only view of an approval request returned by the API.
/// </summary>
public sealed record ApprovalResponse(
    /// <summary>Unique identifier for this approval request.</summary>
    string RequestId,

    /// <summary>Source directive ID.</summary>
    string DirectiveId,

    /// <summary>The write-capable action type that requires human approval.</summary>
    OperatorActionType ActionType,

    /// <summary>Serialized action payload.</summary>
    string Payload,

    /// <summary>UTC timestamp when this request was created.</summary>
    DateTime CreatedAt,

    /// <summary>Current lifecycle status of the approval request.</summary>
    ApprovalStatus Status,

    /// <summary>Human operator who approved the request, if approved.</summary>
    string? ApprovedBy,

    /// <summary>UTC timestamp of approval, if approved.</summary>
    DateTime? ApprovedAt,

    /// <summary>UTC timestamp of rejection, if rejected.</summary>
    DateTime? RejectedAt,

    /// <summary>Optional reason provided at rejection time.</summary>
    string? RejectionReason,

    /// <summary>Optional originating domain.</summary>
    string? Domain = null
);
