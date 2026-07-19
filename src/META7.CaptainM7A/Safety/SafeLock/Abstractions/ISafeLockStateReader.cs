using META7.CaptainM7A.Safety.SafeLock.Domain;

namespace META7.CaptainM7A.Safety.SafeLock.Abstractions;

public interface ISafeLockStateReader
{
    SafeLockSnapshot Snapshot { get; }
    bool IsActive { get; }
    long Version { get; }
}
