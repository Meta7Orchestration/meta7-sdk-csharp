namespace META7.Operator.Api.Execution;

using META7.Operator.Contracts;

/// <summary>
/// Executor that accepts only safe, read-only action types.
/// Does NOT open a browser or perform any external actions —
/// all results are deterministic and based on the directive payload alone.
/// </summary>
public sealed class ReadOnlyOperatorExecutor : IOperatorExecutor
{
    private static readonly HashSet<OperatorActionType> SafeActions = new()
    {
        OperatorActionType.HealthCheck,
        OperatorActionType.Navigate,
        OperatorActionType.ReadPage,
        OperatorActionType.ExtractStructuredData,
        OperatorActionType.TakeScreenshot,
        OperatorActionType.WaitForElement,
    };

    public Task<OperatorResult> ExecuteAsync(
        OperatorDirective directive,
        CancellationToken cancellationToken = default)
    {
        if (!SafeActions.Contains(directive.ActionType))
        {
            return Task.FromResult(new OperatorResult
            {
                DirectiveId = directive.Id,
                Status = OperatorExecutionStatus.Rejected,
                Message = $"Action '{directive.ActionType}' is not permitted by the read-only executor."
            });
        }

        var result = directive.ActionType switch
        {
            OperatorActionType.HealthCheck => new OperatorResult
            {
                DirectiveId = directive.Id,
                Status = OperatorExecutionStatus.Succeeded,
                Message = "Health check passed."
            },
            OperatorActionType.Navigate => new OperatorResult
            {
                DirectiveId = directive.Id,
                Status = OperatorExecutionStatus.Succeeded,
                Message = $"Navigation to '{directive.TargetUrl}' acknowledged (read-only mode)."
            },
            OperatorActionType.ReadPage => new OperatorResult
            {
                DirectiveId = directive.Id,
                Status = OperatorExecutionStatus.Succeeded,
                Message = $"Page read acknowledged for '{directive.TargetUrl}' (read-only mode).",
                Artifacts = new List<OperatorArtifact>
                {
                    new() { Name = "page-content", ContentType = "text/plain", Content = "[read-only stub]" }
                }
            },
            OperatorActionType.ExtractStructuredData => new OperatorResult
            {
                DirectiveId = directive.Id,
                Status = OperatorExecutionStatus.Succeeded,
                Message = "Structured data extraction acknowledged (read-only mode).",
                Artifacts = new List<OperatorArtifact>
                {
                    new() { Name = "structured-data", ContentType = "application/json", Content = "{}" }
                }
            },
            OperatorActionType.TakeScreenshot => new OperatorResult
            {
                DirectiveId = directive.Id,
                Status = OperatorExecutionStatus.Succeeded,
                Message = "Screenshot acknowledged (read-only mode).",
                Artifacts = new List<OperatorArtifact>
                {
                    new() { Name = "screenshot", ContentType = "image/png", Content = string.Empty }
                }
            },
            OperatorActionType.WaitForElement => new OperatorResult
            {
                DirectiveId = directive.Id,
                Status = OperatorExecutionStatus.Succeeded,
                Message = "WaitForElement acknowledged (read-only mode)."
            },
            _ => new OperatorResult
            {
                DirectiveId = directive.Id,
                Status = OperatorExecutionStatus.Rejected,
                Message = $"Unhandled action type '{directive.ActionType}'."
            }
        };

        return Task.FromResult(result);
    }
}
