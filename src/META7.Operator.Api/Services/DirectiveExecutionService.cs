// ══════════════════════════════════════════════════════════════════════════════
// META7 Operator API — Directive Execution Service
// Human Approval Layer: routes every write-capable action through
// HumanApprovalGateway, then executes only when a human approves.
// ══════════════════════════════════════════════════════════════════════════════

using META7.Operator.Api.Approval;
using META7.Operator.Contracts.Approval;

namespace META7.Operator.Api.Services;

/// <summary>
/// Result of a directive execution attempt.
/// </summary>
public sealed record DirectiveExecutionResult(
    bool   Succeeded,
    string RequestId,
    string Reason);

/// <summary>
/// Routes operator directives through the approval and safety pipeline:
/// <list type="number">
///   <item>SAFE_LOCK check — all writes blocked when active.</item>
///   <item>Write-action check — non-write actions are executed immediately.</item>
///   <item>HumanApprovalGateway interception — action queued, returns Pending.</item>
///   <item>Execution only after human approval — triggered by the controller.</item>
/// </list>
/// </summary>
public class DirectiveExecutionService
{
    private readonly HumanApprovalGateway  _gateway;
    private readonly HumanApprovalQueue    _queue;
    private readonly IRevenueActionExecutor _executor;
    private readonly ISafeLockProvider     _safeLock;

    public DirectiveExecutionService(
        HumanApprovalGateway   gateway,
        HumanApprovalQueue     queue,
        IRevenueActionExecutor executor,
        ISafeLockProvider      safeLock)
    {
        _gateway  = gateway  ?? throw new ArgumentNullException(nameof(gateway));
        _queue    = queue    ?? throw new ArgumentNullException(nameof(queue));
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        _safeLock = safeLock ?? throw new ArgumentNullException(nameof(safeLock));
    }

    // ── Submission ────────────────────────────────────────────────────────────

    /// <summary>
    /// Submit a directive action for processing.
    /// <para>
    /// Write-capable actions are intercepted by <see cref="HumanApprovalGateway"/>
    /// and placed in the approval queue. They are NOT executed until a human
    /// operator explicitly approves them via
    /// <see cref="ExecuteApproved(string, string)"/>.
    /// </para>
    /// </summary>
    /// <returns>
    /// A <see cref="DirectiveExecutionResult"/> describing the outcome.
    /// For write actions, <see cref="DirectiveExecutionResult.Succeeded"/> is
    /// <c>true</c> with the queue's request ID, and Reason == "Pending".
    /// </returns>
    public DirectiveExecutionResult Submit(
        string             directiveId,
        OperatorActionType actionType,
        string             payload,
        string?            domain    = null,
        string?            requestId = null,
        DateTime?          createdAt = null)
    {
        if (_safeLock.IsSafeLockActive)
            return new DirectiveExecutionResult(
                false,
                string.Empty,
                "SAFE_LOCK active — all write actions are blocked");

        if (!_gateway.IsWriteAction(actionType))
            return new DirectiveExecutionResult(
                true,
                directiveId,
                $"Read-only action {actionType} executed without approval");

        var request = _gateway.Intercept(
            directiveId, actionType, payload, domain, requestId, createdAt);

        return new DirectiveExecutionResult(
            true,
            request.RequestId,
            "Pending — awaiting human approval");
    }

    // ── Post-approval execution ───────────────────────────────────────────────

    /// <summary>
    /// Execute a previously-approved action.
    /// Called internally by <see cref="META7.Operator.Api.Approval.HumanApprovalController"/>
    /// after a human operator approves the request.
    /// </summary>
    /// <param name="requestId">The request to execute.</param>
    /// <param name="approvedBy">The operator who approved it.</param>
    /// <returns>
    /// A <see cref="DirectiveExecutionResult"/> describing the execution outcome.
    /// Returns failure when SAFE_LOCK is active, when the request is not found,
    /// or when the request is not in Approved state.
    /// </returns>
    public DirectiveExecutionResult ExecuteApproved(string requestId, string approvedBy)
    {
        if (_safeLock.IsSafeLockActive)
            return new DirectiveExecutionResult(
                false,
                requestId,
                "SAFE_LOCK active — execution blocked even after approval");

        var record = _queue.GetById(requestId);
        if (record is null)
            return new DirectiveExecutionResult(
                false,
                requestId,
                $"No approval record found for {requestId}");

        if (record.Status != HumanApprovalStatus.Approved)
            return new DirectiveExecutionResult(
                false,
                requestId,
                $"Request {requestId} is in state {record.Status} — only Approved requests can be executed");

        var ok = _executor.Execute(requestId, record.ActionType, record.Payload);
        return new DirectiveExecutionResult(
            ok,
            requestId,
            ok ? $"Executed by {approvedBy}" : "Executor returned failure");
    }
}
