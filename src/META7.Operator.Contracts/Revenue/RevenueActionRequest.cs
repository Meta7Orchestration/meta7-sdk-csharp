using System.Collections.ObjectModel;

namespace META7.Operator.Contracts.Revenue;

public sealed record RevenueActionRequest
{
    public RevenueActionType ActionType { get; init; }

    public required Uri TargetUrl { get; init; }

    public IReadOnlyDictionary<string, string> FormData { get; init; } =
        new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(StringComparer.Ordinal));

    public required string CorrelationId { get; init; }

    public TimeSpan DirectiveTimeout { get; init; } = TimeSpan.FromSeconds(10);

    public bool RequiresAuthentication { get; init; }

    public bool ContainsPrivateData { get; init; }
}
