namespace META7.Operator.Api.Services;

using META7.CaptainM7A;
using META7.Operator.Api.Execution;
using META7.Operator.Api.Policies;
using META7.Operator.Contracts;

/// <summary>
/// Orchestrates directive execution by applying policy checks, verifying SAFE_LOCK state,
/// and delegating to the configured <see cref="IOperatorExecutor"/>.
/// </summary>
public sealed class DirectiveExecutionService
{
    private readonly OperatorPolicyGate _policyGate;
    private readonly ISafeLockStateReader _safeLockReader;
    private readonly IOperatorExecutor _executor;

    public DirectiveExecutionService(
        OperatorPolicyGate policyGate,
        ISafeLockStateReader safeLockReader,
        IOperatorExecutor executor)
    {
        _policyGate = policyGate;
        _safeLockReader = safeLockReader;
        _executor = executor;
    }

    /// <summary>
    /// Processes a directive through policy validation, SAFE_LOCK check, and executor.
    /// </summary>
    public async Task<OperatorResult> ExecuteAsync(
        OperatorDirective directive,
        CancellationToken cancellationToken = default)
    {
        // 1. Policy gate: check expiry and domain allowlist
        var policy = _policyGate.Evaluate(directive);
        if (!policy.IsAllowed)
        {
            return new OperatorResult
            {
                DirectiveId = directive.Id,
                Status = policy.RejectionStatus,
                Message = policy.Reason
            };
        }

        // 2. SAFE_LOCK boundary check
        if (_safeLockReader.IsSafeLockActive)
        {
            return new OperatorResult
            {
                DirectiveId = directive.Id,
                Status = OperatorExecutionStatus.SafeLocked,
                Message = "SAFE_LOCK is active — directive processing suspended."
            };
        }

        // 3. Delegate to executor
        return await _executor.ExecuteAsync(directive, cancellationToken);
    }
}
