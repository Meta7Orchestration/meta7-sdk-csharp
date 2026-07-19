using System.Collections.Generic;

namespace META7.Operator.Contracts.Outreach;

public enum OutreachUrgencyLevel
{
    Low,
    Medium,
    High,
    Critical
}

public sealed record OutreachContext(
    string SourceUrl,
    string DomainCategory,
    OutreachUrgencyLevel UrgencyLevel,
    IReadOnlyList<string> PainPoints,
    IReadOnlyList<OutreachSignal> Signals);
