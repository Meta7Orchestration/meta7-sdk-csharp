using META7.Operator.Api.Execution;

namespace META7.Operator.Api.Services;

public sealed class DirectiveExecutionService
{
    private readonly ISafeLockState _safeLockState;
    private readonly IOperatorExecutor _playwrightOperatorExecutor;

    public DirectiveExecutionService(ISafeLockState safeLockState, IOperatorExecutor playwrightOperatorExecutor)
    {
        _safeLockState = safeLockState;
        _playwrightOperatorExecutor = playwrightOperatorExecutor;
    }

    public Task<OperatorResult> ExecuteAsync(OperatorDirective directive, CancellationToken cancellationToken)
    {
        if (_safeLockState.IsLocked)
        {
            return Task.FromResult(new OperatorResult(false, "SAFE_LOCK active. Directive rejected.", new(), []));
        }

        return directive.ActionType switch
        {
            OperatorActionType.Navigate => _playwrightOperatorExecutor.ExecuteAsync(directive, cancellationToken),
            OperatorActionType.ReadPage => _playwrightOperatorExecutor.ExecuteAsync(directive, cancellationToken),
            OperatorActionType.ExtractStructuredData => _playwrightOperatorExecutor.ExecuteAsync(directive, cancellationToken),
            OperatorActionType.WaitForElement => _playwrightOperatorExecutor.ExecuteAsync(directive, cancellationToken),
            OperatorActionType.TakeScreenshot => _playwrightOperatorExecutor.ExecuteAsync(directive, cancellationToken),
            _ => Task.FromResult(new OperatorResult(false, "Unsupported operator action.", new(), []))
        };
    }
}
