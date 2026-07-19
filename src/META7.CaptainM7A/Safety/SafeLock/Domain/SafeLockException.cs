namespace META7.CaptainM7A.Safety.SafeLock.Domain;

public sealed class SafeLockException : InvalidOperationException
{
    public SafeLockException(string message, long expectedVersion, long actualVersion)
        : base(message)
    {
        ExpectedVersion = expectedVersion;
        ActualVersion = actualVersion;
    }

    public long ExpectedVersion { get; }
    public long ActualVersion { get; }
}
