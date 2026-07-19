using META7.Operator.Api.Revenue.Models;
using META7.Operator.Contracts.Revenue;

namespace META7.Operator.Api.Revenue;

public sealed record OutreachSuggestion(string Message, double LeadIntentScore, string ValuePropositionKeyword, Uri PublicEndpoint);

public sealed record CommunityContext(string AudienceSegment, int UrgencySignal, int EngagementSignal, bool HasExplicitBuyingIntent);

public sealed class RevenueOpportunityDetector
{
    public RevenueOpportunity Detect(OutreachSuggestion suggestion, CommunityContext context)
    {
        var leadPotential = Math.Clamp((suggestion.LeadIntentScore + (context.EngagementSignal / 100d)) / 2d, 0d, 1d);
        var urgency = Math.Clamp(context.UrgencySignal, 0, 100);
        var valuePropositionMatch = Math.Clamp(CalculateValuePropositionMatch(suggestion.ValuePropositionKeyword, context.AudienceSegment), 0d, 1d);
        var recommendedAction = RecommendActionType(leadPotential, urgency, context.HasExplicitBuyingIntent);

        return new RevenueOpportunity(
            leadPotential,
            urgency,
            valuePropositionMatch,
            recommendedAction,
            $"LeadPotential={leadPotential:F2};Urgency={urgency};ValueMatch={valuePropositionMatch:F2}");
    }

    private static RevenueActionType RecommendActionType(double leadPotential, int urgency, bool hasBuyingIntent)
    {
        if (hasBuyingIntent && urgency >= 80) return RevenueActionType.RequestCallback;
        if (leadPotential >= 0.75) return RevenueActionType.SubmitLeadForm;
        if (urgency >= 70) return RevenueActionType.CreateSupportTicket;
        if (leadPotential >= 0.50) return RevenueActionType.RegisterInterest;
        return RevenueActionType.TriggerWebhook;
    }

    private static double CalculateValuePropositionMatch(string keyword, string segment)
    {
        if (string.IsNullOrWhiteSpace(keyword) || string.IsNullOrWhiteSpace(segment))
        {
            return 0.3;
        }

        var keywordTokens = keyword.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var segmentLower = segment.ToLowerInvariant();
        var hits = keywordTokens.Count(token => segmentLower.Contains(token.ToLowerInvariant(), StringComparison.Ordinal));

        return keywordTokens.Length == 0 ? 0.3 : (double)hits / keywordTokens.Length;
    }
}
