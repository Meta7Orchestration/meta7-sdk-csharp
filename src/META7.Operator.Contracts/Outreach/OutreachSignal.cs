using System.Collections.Generic;

namespace META7.Operator.Contracts.Outreach;

public sealed record OutreachSignal(
    string SourceUrl,
    string MatchedKeyword,
    string MatchedText,
    string SignalType,
    double Confidence,
    IReadOnlyDictionary<string, string> Metadata);
