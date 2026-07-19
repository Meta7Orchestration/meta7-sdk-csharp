namespace META7.Operator.Api.Outreach;

public interface ISafeLockStateProvider
{
    bool IsSafeLockActive { get; }
}
