using META7.CaptainM7A.Safety.SafeLock.Domain;
using META7.CaptainM7A.Safety.SafeLock.Events;

namespace META7.CaptainM7A.Safety.SafeLock.Abstractions;

public interface ISafeLock : ISafeLockStateReader
{
    SafeLockActivated Activate(SafeLockReason reason, string actorId, string auditMessage, string? reasonDetail = null);
    SafeLockReleased Release(long expectedVersion, SafeLockReason reason, string actorId, string auditMessage, string? reasonDetail = null);
}
