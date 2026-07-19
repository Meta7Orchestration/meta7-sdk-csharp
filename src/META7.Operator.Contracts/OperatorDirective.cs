namespace META7.Operator.Contracts;

/// <summary>
/// A directive issued by the Captain SDK to the Operator API.
/// </summary>
public sealed class OperatorDirective
{
    /// <summary>Unique identifier of this directive.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>The action the Operator should perform.</summary>
    public OperatorActionType ActionType { get; init; }

    /// <summary>Target URL on which the action operates.</summary>
    public string TargetUrl { get; init; } = string.Empty;

    /// <summary>UTC time at which this directive was issued.</summary>
    public DateTime IssuedAt { get; init; }

    /// <summary>UTC deadline after which this directive must be rejected.</summary>
    public DateTime ExpiresAt { get; init; }

    /// <summary>
    /// Explicit domain allowlist. The host portion of <see cref="TargetUrl"/>
    /// must appear in this list for the directive to be accepted.
    /// </summary>
    public List<string> AllowedDomains { get; init; } = new();

    /// <summary>Optional action-specific parameters.</summary>
    public Dictionary<string, string>? Parameters { get; init; }
}
