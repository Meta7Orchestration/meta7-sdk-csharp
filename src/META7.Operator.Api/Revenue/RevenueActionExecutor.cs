using System.Security.Cryptography;
using System.Text;
using META7.Operator.Api.Revenue.Models;
using META7.Operator.Contracts.Revenue;

namespace META7.Operator.Api.Revenue;

public interface ISafeLockStateProvider
{
    bool IsSafeLockActive { get; }
}

public interface IPlaywrightOperatorExecutor
{
    Task<RevenueActionResult> ExecuteWriteActionAsync(RevenueActionRequest request, CancellationToken cancellationToken);
}

public sealed class RevenueActionExecutor
{
    private readonly ISafeLockStateProvider _safeLock;
    private readonly RevenueSafetyGate _safetyGate;
    private readonly IPlaywrightOperatorExecutor _playwrightOperatorExecutor;

    public RevenueActionExecutor(
        ISafeLockStateProvider safeLock,
        RevenueSafetyGate safetyGate,
        IPlaywrightOperatorExecutor playwrightOperatorExecutor)
    {
        _safeLock = safeLock;
        _safetyGate = safetyGate;
        _playwrightOperatorExecutor = playwrightOperatorExecutor;
    }

    public async Task<RevenueActionResponse> ExecuteAsync(RevenueActionRequest request, CancellationToken cancellationToken = default)
    {
        if (_safeLock.IsSafeLockActive)
        {
            return BlockedResponse(request, "SAFE_LOCK active.");
        }

        var safetyDecision = _safetyGate.Validate(request);
        if (!safetyDecision.IsAllowed)
        {
            return BlockedResponse(request, safetyDecision.Reason);
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(request.DirectiveTimeout <= TimeSpan.Zero ? TimeSpan.FromSeconds(1) : request.DirectiveTimeout);

        try
        {
            var result = await _playwrightOperatorExecutor.ExecuteWriteActionAsync(request, timeoutCts.Token).ConfigureAwait(false);
            var executionReference = string.IsNullOrWhiteSpace(result.ExecutionReference)
                ? BuildDeterministicExecutionReference(request)
                : result.ExecutionReference;

            return new RevenueActionResponse(
                request.ActionType,
                IsAllowed: true,
                IsExecuted: true,
                Reason: "Executed within bounded revenue policy.",
                ExecutionReference: executionReference,
                TargetDomain: request.TargetUrl.Host.ToLowerInvariant());
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            return BlockedResponse(request, "Directive timeout exceeded.");
        }
    }

    private static RevenueActionResponse BlockedResponse(RevenueActionRequest request, string reason) => new(
        request.ActionType,
        IsAllowed: false,
        IsExecuted: false,
        Reason: reason,
        ExecutionReference: BuildDeterministicExecutionReference(request),
        TargetDomain: request.TargetUrl.Host.ToLowerInvariant());

    private static string BuildDeterministicExecutionReference(RevenueActionRequest request)
    {
        var canonicalFields = string.Join("|", request.FormData
            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .ThenBy(kv => kv.Value, StringComparer.Ordinal)
            .Select(kv => $"{kv.Key}={kv.Value}"));
        var payload = $"{request.ActionType}|{request.TargetUrl}|{request.CorrelationId}|{canonicalFields}";

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash);
    }
}
