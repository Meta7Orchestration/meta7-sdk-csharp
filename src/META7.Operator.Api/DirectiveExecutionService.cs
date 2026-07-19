using META7.Operator.Api.Revenue;
using META7.Operator.Contracts;
using META7.Operator.Contracts.Revenue;

namespace META7.Operator.Api;

public sealed class DirectiveExecutionService
{
    private readonly ISafeLockStateProvider _safeLock;
    private readonly RevenueSafetyGate _revenueSafetyGate;
    private readonly RevenueActionExecutor _revenueActionExecutor;

    public DirectiveExecutionService(
        ISafeLockStateProvider safeLock,
        RevenueSafetyGate revenueSafetyGate,
        RevenueActionExecutor revenueActionExecutor)
    {
        _safeLock = safeLock;
        _revenueSafetyGate = revenueSafetyGate;
        _revenueActionExecutor = revenueActionExecutor;
    }

    public Task<RevenueActionResponse> ExecuteAsync(
        OperatorActionType actionType,
        Uri targetUrl,
        IReadOnlyDictionary<string, string> formData,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        if (!actionType.IsRevenueWriteAction())
        {
            return Task.FromResult(new RevenueActionResponse(
                RevenueActionType.TriggerWebhook,
                IsAllowed: false,
                IsExecuted: false,
                Reason: "Action type is not a bounded revenue write action.",
                ExecutionReference: correlationId,
                TargetDomain: targetUrl.Host.ToLowerInvariant()));
        }

        var revenueRequest = new RevenueActionRequest
        {
            ActionType = actionType.ToRevenueActionType(),
            TargetUrl = targetUrl,
            FormData = formData,
            CorrelationId = correlationId,
            DirectiveTimeout = TimeSpan.FromSeconds(10),
            RequiresAuthentication = false,
            ContainsPrivateData = false
        };

        if (_safeLock.IsSafeLockActive)
        {
            return Task.FromResult(new RevenueActionResponse(
                revenueRequest.ActionType,
                IsAllowed: false,
                IsExecuted: false,
                Reason: "SAFE_LOCK active.",
                ExecutionReference: correlationId,
                TargetDomain: targetUrl.Host.ToLowerInvariant()));
        }

        var decision = _revenueSafetyGate.Validate(revenueRequest);
        if (!decision.IsAllowed)
        {
            return Task.FromResult(new RevenueActionResponse(
                revenueRequest.ActionType,
                IsAllowed: false,
                IsExecuted: false,
                Reason: decision.Reason,
                ExecutionReference: correlationId,
                TargetDomain: targetUrl.Host.ToLowerInvariant()));
        }

        return _revenueActionExecutor.ExecuteAsync(revenueRequest, cancellationToken);
    }
}
