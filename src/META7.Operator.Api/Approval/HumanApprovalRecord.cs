// ══════════════════════════════════════════════════════════════════════════════
// META7 Operator API — Human Approval Record
// Human Approval Layer: immutable audit record for a single write-action request.
// Becomes permanently sealed once a human approves or rejects it.
// ══════════════════════════════════════════════════════════════════════════════

using META7.Operator.Contracts.Approval;

namespace META7.Operator.Api.Approval;

/// <summary>
/// Audit record for a write-capable action awaiting (or having received) a
/// human decision. The record is immutable after approval or rejection.
/// </summary>
public sealed class HumanApprovalRecord
{
    // ── Identity ──────────────────────────────────────────────────────────────

    /// <summary>Unique request identifier (GUID).</summary>
    public string RequestId { get; }

    /// <summary>Source directive that triggered this action.</summary>
    public string DirectiveId { get; }

    /// <summary>The write-capable action type that requires human approval.</summary>
    public OperatorActionType ActionType { get; }

    /// <summary>Serialised action payload supplied at gateway interception.</summary>
    public string Payload { get; }

    /// <summary>Optional originating domain (used for allowlist enforcement).</summary>
    public string? Domain { get; }

    // ── Timestamps ────────────────────────────────────────────────────────────

    /// <summary>UTC timestamp when the approval request was created.</summary>
    public DateTime CreatedAt { get; }

    /// <summary>UTC timestamp of human approval. Null until explicitly approved.</summary>
    public DateTime? ApprovedAt { get; private set; }

    /// <summary>UTC timestamp of human rejection. Null until explicitly rejected.</summary>
    public DateTime? RejectedAt { get; private set; }

    // ── Decision ──────────────────────────────────────────────────────────────

    /// <summary>Human operator who approved the request. Null until explicitly approved.</summary>
    public string? ApprovedBy { get; private set; }

    /// <summary>Optional reason captured at rejection time.</summary>
    public string? RejectionReason { get; private set; }

    /// <summary>Internal lifecycle status.</summary>
    public HumanApprovalStatus Status { get; private set; } = HumanApprovalStatus.Pending;

    // ── Constructor ───────────────────────────────────────────────────────────

    public HumanApprovalRecord(
        string requestId,
        string directiveId,
        OperatorActionType actionType,
        string payload,
        DateTime createdAt,
        string? domain = null)
    {
        RequestId   = requestId   ?? throw new ArgumentNullException(nameof(requestId));
        DirectiveId = directiveId ?? throw new ArgumentNullException(nameof(directiveId));
        Payload     = payload     ?? throw new ArgumentNullException(nameof(payload));
        ActionType  = actionType;
        CreatedAt   = createdAt;
        Domain      = domain;
    }

    // ── State transitions ─────────────────────────────────────────────────────

    /// <summary>
    /// Mark this record as approved by a named human operator.
    /// Throws <see cref="InvalidOperationException"/> if already decided.
    /// </summary>
    public void Approve(string approvedBy, DateTime? approvedAt = null)
    {
        EnsurePending();
        ApprovedBy  = approvedBy ?? throw new ArgumentNullException(nameof(approvedBy));
        ApprovedAt  = approvedAt ?? DateTime.UtcNow;
        Status      = HumanApprovalStatus.Approved;
    }

    /// <summary>
    /// Mark this record as rejected.
    /// Throws <see cref="InvalidOperationException"/> if already decided.
    /// </summary>
    public void Reject(string? reason = null, DateTime? rejectedAt = null)
    {
        EnsurePending();
        RejectionReason = reason;
        RejectedAt      = rejectedAt ?? DateTime.UtcNow;
        Status          = HumanApprovalStatus.Rejected;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void EnsurePending()
    {
        if (Status != HumanApprovalStatus.Pending)
            throw new InvalidOperationException(
                $"Approval record {RequestId} is already in terminal state {Status}. " +
                "Records are immutable after a human decision.");
    }

    /// <summary>
    /// Project this internal record to the public-facing contract DTO.
    /// </summary>
    public Contracts.Approval.ApprovalResponse ToResponse() =>
        new(
            RequestId:       RequestId,
            DirectiveId:     DirectiveId,
            ActionType:      ActionType,
            Payload:         Payload,
            CreatedAt:       CreatedAt,
            Status:          (Contracts.Approval.ApprovalStatus)(int)Status,
            ApprovedBy:      ApprovedBy,
            ApprovedAt:      ApprovedAt,
            RejectedAt:      RejectedAt,
            RejectionReason: RejectionReason,
            Domain:          Domain);
}
