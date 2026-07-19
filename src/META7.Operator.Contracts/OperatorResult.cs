namespace META7.Operator.Contracts;

/// <summary>
/// The deterministic result returned by the Operator API after processing a directive.
/// </summary>
public sealed class OperatorResult
{
    /// <summary>The identifier of the directive that produced this result.</summary>
    public string DirectiveId { get; init; } = string.Empty;

    /// <summary>Terminal status of the execution.</summary>
    public OperatorExecutionStatus Status { get; init; }

    /// <summary>Human-readable message describing the outcome.</summary>
    public string? Message { get; init; }

    /// <summary>Artifacts produced during execution, if any.</summary>
    public List<OperatorArtifact>? Artifacts { get; init; }
}
