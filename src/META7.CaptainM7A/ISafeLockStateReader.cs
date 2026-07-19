namespace META7.CaptainM7A
{
    /// <summary>
    /// Read-only adapter boundary for querying SAFE_LOCK state.
    /// Operator.Api consumes this interface to avoid taking a direct
    /// dependency on any concrete SafeLock implementation.
    /// </summary>
    public interface ISafeLockStateReader
    {
        /// <summary>Gets a value indicating whether SAFE_LOCK is currently active.</summary>
        bool IsSafeLockActive { get; }
    }

    /// <summary>
    /// Default adapter that delegates to <see cref="CanonicalEventStore.SafeLockActive"/>.
    /// Register this in DI when you want the live store to drive the Operator.
    /// </summary>
    public sealed class CanonicalEventStoreSafeLockAdapter : ISafeLockStateReader
    {
        private readonly CanonicalEventStore _store;

        public CanonicalEventStoreSafeLockAdapter(CanonicalEventStore store)
        {
            _store = store;
        }

        public bool IsSafeLockActive => _store.SafeLockActive;
    }
}
