namespace META7.Operator.Api.Execution;

using META7.Operator.Contracts;

/// <summary>
/// Executes an <see cref="OperatorDirective"/> and returns an <see cref="OperatorResult"/>.
/// </summary>
public interface IOperatorExecutor
{
    Task<OperatorResult> ExecuteAsync(OperatorDirective directive, CancellationToken cancellationToken = default);
}
