// ══════════════════════════════════════════════════════════════════════════════
// META7 Operator Contracts — Operator Action Types
// Human Approval Layer: write-capable actions require explicit human sign-off.
// ══════════════════════════════════════════════════════════════════════════════

namespace META7.Operator.Contracts.Approval;

/// <summary>
/// All action types the Autonomous Revenue Engine may request.
/// Write-capable actions (value >= 100) are APPROVAL-REQUIRED and must pass
/// through HumanApprovalGateway before any execution occurs.
/// </summary>
public enum OperatorActionType
{
    // ── Read-only (no approval needed) ───────────────────────────────────────

    /// <summary>Query the current system status. No external state is modified.</summary>
    QueryStatus = 0,

    /// <summary>Retrieve operational metrics. No external state is modified.</summary>
    GetMetrics  = 1,

    // ── Write-capable (APPROVAL REQUIRED) ────────────────────────────────────
    // These actions modify external state. HumanApprovalGateway intercepts
    // every one and holds it in HumanApprovalQueue until a human operator
    // explicitly approves or rejects via POST /v1/approvals/{id}/approve|reject.

    /// <summary>Submit a lead form to an external target. APPROVAL REQUIRED.</summary>
    SubmitLeadForm      = 100,

    /// <summary>Request a human callback on behalf of the system. APPROVAL REQUIRED.</summary>
    RequestCallback     = 101,

    /// <summary>Trigger an outbound webhook. APPROVAL REQUIRED.</summary>
    TriggerWebhook      = 102,

    /// <summary>Create a support ticket in an external system. APPROVAL REQUIRED.</summary>
    CreateSupportTicket = 103,

    /// <summary>Register interest in an external product or service. APPROVAL REQUIRED.</summary>
    RegisterInterest    = 104,
}
