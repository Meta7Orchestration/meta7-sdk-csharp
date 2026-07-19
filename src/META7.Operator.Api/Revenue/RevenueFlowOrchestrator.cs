using META7.Operator.Api.Revenue.Models;
using META7.Operator.Contracts.Revenue;

namespace META7.Operator.Api.Revenue;

public sealed class RevenueFlowOrchestrator
{
    private readonly RevenueOpportunityDetector _detector;
    private readonly RevenueActionExecutor _executor;

    public RevenueFlowOrchestrator(RevenueOpportunityDetector detector, RevenueActionExecutor executor)
    {
        _detector = detector;
        _executor = executor;
    }

    public RevenueActionRequest BuildActionRequest(OutreachSuggestion suggestion, CommunityContext context, string correlationId)
    {
        var opportunity = _detector.Detect(suggestion, context);
        var formData = BuildDeterministicFormData(suggestion, context, opportunity);

        return new RevenueActionRequest
        {
            ActionType = opportunity.RecommendedActionType,
            TargetUrl = suggestion.PublicEndpoint,
            FormData = formData,
            CorrelationId = correlationId,
            DirectiveTimeout = TimeSpan.FromSeconds(10),
            RequiresAuthentication = false,
            ContainsPrivateData = false
        };
    }

    public Task<RevenueActionResponse> ExecuteAsync(
        OutreachSuggestion suggestion,
        CommunityContext context,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var request = BuildActionRequest(suggestion, context, correlationId);
        return _executor.ExecuteAsync(request, cancellationToken);
    }

    private static IReadOnlyDictionary<string, string> BuildDeterministicFormData(
        OutreachSuggestion suggestion,
        CommunityContext context,
        RevenueOpportunity opportunity)
    {
        var fields = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["name"] = context.AudienceSegment,
            ["email"] = $"{Sanitize(context.AudienceSegment)}@example.com",
            ["message"] = suggestion.Message,
            ["value_proposition"] = suggestion.ValuePropositionKeyword,
            ["opportunity_summary"] = opportunity.Summary
        };

        return fields;
    }

    private static string Sanitize(string input)
    {
        var chars = input.Where(char.IsLetterOrDigit).ToArray();
        return chars.Length == 0 ? "contact" : new string(chars).ToLowerInvariant();
    }
}
