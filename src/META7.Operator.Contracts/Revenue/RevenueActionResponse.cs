namespace META7.Operator.Contracts.Revenue;

public sealed record RevenueActionResponse(
    RevenueActionType ActionType,
    bool IsAllowed,
    bool IsExecuted,
    string Reason,
    string ExecutionReference,
    string TargetDomain);
