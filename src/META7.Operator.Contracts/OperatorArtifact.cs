namespace META7.Operator.Contracts;

/// <summary>
/// An artifact produced by an Operator directive execution (e.g. a screenshot, extracted text).
/// </summary>
public sealed class OperatorArtifact
{
    /// <summary>Logical name of the artifact (e.g. "screenshot", "page-text").</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>MIME content type (e.g. "image/png", "text/plain", "application/json").</summary>
    public string ContentType { get; init; } = string.Empty;

    /// <summary>
    /// Artifact content. For binary data (e.g. screenshots) this is a base-64 encoded string.
    /// For textual data it is the raw content.
    /// </summary>
    public string Content { get; init; } = string.Empty;
}
