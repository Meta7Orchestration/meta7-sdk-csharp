namespace META7.Operator.Contracts.Outreach;

public sealed record OutreachSuggestion(
    string ContextSummary,
    string DetectedPainPoint,
    string RecommendedValueProposition,
    double ConfidenceScore);
