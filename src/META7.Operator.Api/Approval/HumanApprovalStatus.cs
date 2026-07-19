// ══════════════════════════════════════════════════════════════════════════════
// META7 Operator API — Human Approval Status (internal)
// Human Approval Layer: internal lifecycle enum used by HumanApprovalRecord.
// ══════════════════════════════════════════════════════════════════════════════

namespace META7.Operator.Api.Approval;

/// <summary>
/// Internal lifecycle status of a <see cref="HumanApprovalRecord"/>.
/// Maps directly to the public <see cref="META7.Operator.Contracts.Approval.ApprovalStatus"/> DTO.
/// </summary>
public enum HumanApprovalStatus
{
    /// <summary>No human decision has been made. Execution is blocked.</summary>
    Pending  = 0,

    /// <summary>A human operator explicitly approved the action. Execution is permitted.</summary>
    Approved = 1,

    /// <summary>A human operator explicitly rejected the action. Execution is permanently blocked.</summary>
    Rejected = 2,
}
