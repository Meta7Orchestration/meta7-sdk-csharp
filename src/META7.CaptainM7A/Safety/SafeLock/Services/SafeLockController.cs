using META7.CaptainM7A.Safety.SafeLock.Abstractions;
using META7.CaptainM7A.Safety.SafeLock.Domain;
using META7.CaptainM7A.Safety.SafeLock.Events;

namespace META7.CaptainM7A.Safety.SafeLock.Services;

public sealed class SafeLockController : ISafeLock
{
    private readonly object _sync = new();
    private readonly Func<DateTime> _utcNow;
    private SafeLockSnapshot _snapshot;

    public SafeLockController(Func<DateTime>? utcNow = null)
    {
        _utcNow = utcNow ?? (() => DateTime.UtcNow);
        _snapshot = SafeLockSnapshot.Initial(_utcNow());
    }

    public SafeLockSnapshot Snapshot
    {
        get
        {
            lock (_sync)
            {
                return _snapshot;
            }
        }
    }

    public bool IsActive => Snapshot.IsActive;
    public long Version => Snapshot.Version;

    public SafeLockActivated Activate(SafeLockReason reason, string actorId, string auditMessage, string? reasonDetail = null)
    {
        lock (_sync)
        {
            _snapshot = new SafeLockSnapshot(
                SafeLockState.Active,
                _snapshot.Version + 1,
                reason,
                reasonDetail ?? auditMessage,
                actorId,
                auditMessage,
                _utcNow());

            return new SafeLockActivated(_snapshot);
        }
    }

    public SafeLockReleased Release(long expectedVersion, SafeLockReason reason, string actorId, string auditMessage, string? reasonDetail = null)
    {
        lock (_sync)
        {
            if (_snapshot.Version != expectedVersion)
            {
                throw new SafeLockException(
                    $"SAFE_LOCK release expected version {expectedVersion}, actual {_snapshot.Version}",
                    expectedVersion,
                    _snapshot.Version);
            }

            _snapshot = new SafeLockSnapshot(
                SafeLockState.Unlocked,
                _snapshot.Version + 1,
                reason,
                reasonDetail ?? auditMessage,
                actorId,
                auditMessage,
                _utcNow());

            return new SafeLockReleased(_snapshot);
        }
    }
}
