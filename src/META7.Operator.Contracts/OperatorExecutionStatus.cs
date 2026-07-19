namespace META7.Operator.Contracts;

/// <summary>
/// Terminal status of an Operator directive execution.
/// </summary>
public enum OperatorExecutionStatus
{
    /// <summary>Directive completed successfully.</summary>
    Succeeded,

    /// <summary>Directive failed during execution.</summary>
    Failed,

    /// <summary>Directive was rejected by the executor (e.g. unsafe action).</summary>
    Rejected,

    /// <summary>SAFE_LOCK is active; no directives will be processed.</summary>
    SafeLocked,

    /// <summary>Directive was rejected because it has expired.</summary>
    Expired,

    /// <summary>Directive was rejected because it violates operator policy.</summary>
    PolicyViolation
}
