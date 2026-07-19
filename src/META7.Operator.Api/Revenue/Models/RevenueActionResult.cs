namespace META7.Operator.Api.Revenue.Models;

public sealed record RevenueActionResult(
    string ExecutionReference,
    string ActionDigest,
    bool IsDeterministic);
