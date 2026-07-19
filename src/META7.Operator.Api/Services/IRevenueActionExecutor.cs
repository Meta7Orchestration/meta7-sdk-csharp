// ══════════════════════════════════════════════════════════════════════════════
// META7 Operator API — Revenue Action Executor (interface)
// Human Approval Layer: abstraction for the downstream executor, allowing
// deterministic mocking in integration tests.
// ══════════════════════════════════════════════════════════════════════════════

using META7.Operator.Contracts.Approval;

namespace META7.Operator.Api.Services;

/// <summary>
/// Abstraction for executing an approved write-capable action.
/// Implementations must only be invoked AFTER a human approval decision.
/// </summary>
public interface IRevenueActionExecutor
{
    /// <summary>
    /// Execute the approved action described by <paramref name="requestId"/>.
    /// </summary>
    /// <param name="requestId">The approved request ID for audit correlation.</param>
    /// <param name="actionType">The write-capable action to execute.</param>
    /// <param name="payload">Serialised action payload.</param>
    /// <returns>
    /// <c>true</c> if the action was executed successfully; <c>false</c> otherwise.
    /// </returns>
    bool Execute(string requestId, OperatorActionType actionType, string payload);
}
