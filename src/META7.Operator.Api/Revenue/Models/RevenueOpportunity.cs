using META7.Operator.Contracts.Revenue;

namespace META7.Operator.Api.Revenue.Models;

public sealed record RevenueOpportunity(
    double LeadPotential,
    int Urgency,
    double ValuePropositionMatch,
    RevenueActionType RecommendedActionType,
    string Summary);
