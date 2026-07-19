namespace META7.Operator.Api;

public enum OperatorActionType
{
    Navigate,
    ReadPage,
    ExtractStructuredData,
    WaitForElement,
    TakeScreenshot
}

public sealed record OperatorDirective(
    OperatorActionType ActionType,
    string? Url = null,
    string? Selector = null,
    Dictionary<string, string>? Selectors = null,
    int TimeoutMs = 5000);

public sealed record OperatorArtifact(string Name, string ContentType, string ContentBase64);

public sealed record OperatorResult(
    bool Succeeded,
    string Message,
    Dictionary<string, object?> Data,
    IReadOnlyList<OperatorArtifact> Artifacts);

public interface ISafeLockState
{
    bool IsLocked { get; }
}

public sealed class DefaultSafeLockState : ISafeLockState
{
    public bool IsLocked => false;
}

public sealed class OperatorExecutionOptions
{
    public string[] AllowedDomains { get; set; } = ["localhost", "127.0.0.1"];
    public int DefaultTimeoutMs { get; set; } = 5000;
    public int MaxTimeoutMs { get; set; } = 15000;
    public int ViewportWidth { get; set; } = 1280;
    public int ViewportHeight { get; set; } = 720;
    public string UserAgent { get; set; } = "META7-SyntheticOperator/1.0 (Deterministic)";

    public bool IsAllowedDomain(Uri uri)
    {
        if (!uri.IsAbsoluteUri || string.IsNullOrWhiteSpace(uri.Host))
        {
            return false;
        }

        return AllowedDomains.Any(allowed => string.Equals(uri.Host, allowed, StringComparison.OrdinalIgnoreCase));
    }
}
