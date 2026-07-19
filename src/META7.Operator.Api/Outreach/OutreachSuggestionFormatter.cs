using System;
using System.Collections.Generic;
using System.Linq;
using META7.Operator.Contracts.Outreach;

namespace META7.Operator.Api.Outreach;

public sealed class OutreachSuggestionFormatter
{
    public IReadOnlyList<OutreachSuggestion> Format(
        OutreachContext context,
        IReadOnlyList<string> valuePropositions)
    {
        var orderedPainPoints = context.PainPoints.OrderBy(p => p, StringComparer.Ordinal).ToArray();
        if (orderedPainPoints.Length == 0)
        {
            orderedPainPoints = ["UnclassifiedPainPoint"];
        }

        var fallbackValue = valuePropositions.FirstOrDefault() ?? "ระบบแจ้งเตือนออเดอร์ทันที";
        var suggestions = new List<OutreachSuggestion>();

        for (var index = 0; index < orderedPainPoints.Length; index++)
        {
            var value = index < valuePropositions.Count ? valuePropositions[index] : fallbackValue;
            suggestions.Add(new OutreachSuggestion(
                ContextSummary: BuildContextSummary(context),
                DetectedPainPoint: orderedPainPoints[index],
                RecommendedValueProposition: value,
                ConfidenceScore: BuildConfidenceScore(context)));
        }

        return suggestions;
    }

    private static double BuildConfidenceScore(OutreachContext context)
    {
        var urgencyBase = context.UrgencyLevel switch
        {
            OutreachUrgencyLevel.Critical => 0.95d,
            OutreachUrgencyLevel.High => 0.85d,
            OutreachUrgencyLevel.Medium => 0.72d,
            _ => 0.60d
        };

        var signalBoost = Math.Min(0.05d, context.Signals.Count * 0.01d);
        return Math.Round(Math.Min(1.0d, urgencyBase + signalBoost), 2, MidpointRounding.AwayFromZero);
    }

    private static string BuildContextSummary(OutreachContext context) =>
        $"domain={context.DomainCategory};urgency={context.UrgencyLevel};pain_points={string.Join('|', context.PainPoints)}";
}
