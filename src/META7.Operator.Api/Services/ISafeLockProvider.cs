// ══════════════════════════════════════════════════════════════════════════════
// META7 Operator API — Safe Lock Provider (interface)
// Human Approval Layer: abstraction for SAFE_LOCK state, enabling deterministic
// mocking in integration tests.
// ══════════════════════════════════════════════════════════════════════════════

namespace META7.Operator.Api.Services;

/// <summary>
/// Provides the current SAFE_LOCK system state.
/// When SAFE_LOCK is active, ALL write actions are blocked regardless of their
/// approval status.
/// </summary>
public interface ISafeLockProvider
{
    /// <summary>
    /// Returns <c>true</c> when the system is in SAFE_LOCK mode.
    /// All executions must be blocked while this returns <c>true</c>.
    /// </summary>
    bool IsSafeLockActive { get; }
}
