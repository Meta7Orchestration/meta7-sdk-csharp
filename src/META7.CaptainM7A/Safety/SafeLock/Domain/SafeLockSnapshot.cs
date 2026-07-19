namespace META7.CaptainM7A.Safety.SafeLock.Domain;

public record SafeLockSnapshot(
    SafeLockState State,
    long Version,
    SafeLockReason Reason,
    string ReasonDetail,
    string ActorId,
    string AuditMessage,
    DateTime ChangedAt)
{
    public bool IsActive => State == SafeLockState.Active;

    public static SafeLockSnapshot Initial(DateTime changedAt) =>
        new(SafeLockState.Unlocked, 0, SafeLockReason.Unspecified, string.Empty, string.Empty, string.Empty, changedAt);
}
