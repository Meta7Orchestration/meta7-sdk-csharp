using META7.Operator.Contracts.Revenue;

namespace META7.Operator.Api.Revenue;

public sealed record RevenueSafetyDecision(bool IsAllowed, string Reason);

public sealed class RevenueSafetyGate
{
    private static readonly HashSet<RevenueActionType> AllowedActions =
    [
        RevenueActionType.SubmitLeadForm,
        RevenueActionType.RequestCallback,
        RevenueActionType.TriggerWebhook,
        RevenueActionType.CreateSupportTicket,
        RevenueActionType.RegisterInterest
    ];

    private static readonly string[] SensitiveFieldMarkers =
    [
        "password", "passcode", "token", "secret", "session", "auth", "credit", "card", "ssn", "private"
    ];

    private readonly HashSet<string> _domainAllowlist;

    public RevenueSafetyGate(IEnumerable<string> domainAllowlist)
    {
        _domainAllowlist = domainAllowlist
            .Select(d => d.Trim().ToLowerInvariant())
            .Where(d => d.Length > 0)
            .ToHashSet(StringComparer.Ordinal);
    }

    public RevenueSafetyDecision Validate(RevenueActionRequest request)
    {
        if (!AllowedActions.Contains(request.ActionType))
        {
            return new(false, "Action type is outside bounded revenue write actions.");
        }

        if (request.RequiresAuthentication)
        {
            return new(false, "Action requires login and is blocked by policy.");
        }

        if (request.ContainsPrivateData || ContainsSensitiveData(request.FormData))
        {
            return new(false, "Action contains private data and is blocked by policy.");
        }

        if (!_domainAllowlist.Contains(request.TargetUrl.Host.ToLowerInvariant()))
        {
            return new(false, "Target domain is not in allowlist.");
        }

        if (!IsValidFormStructure(request))
        {
            return new(false, "Form structure is invalid for the requested write action.");
        }

        return new(true, "Allowed");
    }

    private static bool IsValidFormStructure(RevenueActionRequest request)
    {
        if (!request.TargetUrl.IsAbsoluteUri || request.TargetUrl.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        if (request.ActionType == RevenueActionType.TriggerWebhook)
        {
            return request.FormData.Count > 0;
        }

        return request.FormData.Count > 0
               && request.FormData.ContainsKey("name")
               && request.FormData.ContainsKey("email");
    }

    private static bool ContainsSensitiveData(IReadOnlyDictionary<string, string> formData)
    {
        foreach (var pair in formData)
        {
            if (SensitiveFieldMarkers.Any(marker =>
                    pair.Key.Contains(marker, StringComparison.OrdinalIgnoreCase)
                    || pair.Value.Contains(marker, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        return false;
    }
}
