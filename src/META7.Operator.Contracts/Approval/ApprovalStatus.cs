// ══════════════════════════════════════════════════════════════════════════════
// META7 Operator Contracts — Approval Status
// Human Approval Layer: every write action requires explicit human sign-off.
// ══════════════════════════════════════════════════════════════════════════════

namespace META7.Operator.Contracts.Approval;

/// <summary>
/// The lifecycle state of a human approval request.
/// </summary>
public enum ApprovalStatus
{
    /// <summary>
    /// The action is awaiting explicit human approval. No execution has occurred.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// A human operator explicitly approved the action. Execution may proceed.
    /// </summary>
    Approved = 1,

    /// <summary>
    /// A human operator explicitly rejected the action. Execution is permanently blocked.
    /// </summary>
    Rejected = 2,
}
