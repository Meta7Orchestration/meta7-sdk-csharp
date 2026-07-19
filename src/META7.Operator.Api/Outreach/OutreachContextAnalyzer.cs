using System;
using System.Collections.Generic;
using System.Linq;
using META7.Operator.Contracts.Outreach;

namespace META7.Operator.Api.Outreach;

public sealed class OutreachContextAnalyzer
{
    private static readonly IReadOnlyDictionary<string, string> PainPointByKeyword =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["need help"] = "NeedSupport",
            ["แจ้งเตือน"] = "OperationalAlert",
            ["ระบบล่ม"] = "SystemOutage",
            ["POS ช้า"] = "SlowPOS",
            ["ออเดอร์ตกหล่น"] = "DroppedOrders"
        };

    public OutreachContext Analyze(IReadOnlyList<OutreachSignal> signals)
    {
        signals ??= [];

        var sourceUrl = signals.FirstOrDefault()?.SourceUrl ?? string.Empty;
        var painPoints = signals
            .Select(signal => PainPointByKeyword.TryGetValue(signal.MatchedKeyword, out var painPoint)
                ? painPoint
                : "UnclassifiedPainPoint")
            .Distinct(StringComparer.Ordinal)
            .OrderBy(painPoint => painPoint, StringComparer.Ordinal)
            .ToArray();

        var urgency = ResolveUrgency(signals);
        var category = ResolveCategory(sourceUrl);

        return new OutreachContext(
            SourceUrl: sourceUrl,
            DomainCategory: category,
            UrgencyLevel: urgency,
            PainPoints: painPoints,
            Signals: signals.OrderBy(signal => signal.MatchedKeyword, StringComparer.Ordinal).ToArray());
    }

    private static OutreachUrgencyLevel ResolveUrgency(IEnumerable<OutreachSignal> signals)
    {
        if (signals.Any(s => s.MatchedKeyword.Equals("ระบบล่ม", StringComparison.OrdinalIgnoreCase)))
        {
            return OutreachUrgencyLevel.Critical;
        }

        if (signals.Any(s => s.MatchedKeyword.Equals("ออเดอร์ตกหล่น", StringComparison.OrdinalIgnoreCase)
            || s.MatchedKeyword.Equals("POS ช้า", StringComparison.OrdinalIgnoreCase)))
        {
            return OutreachUrgencyLevel.High;
        }

        if (signals.Any(s => s.MatchedKeyword.Equals("แจ้งเตือน", StringComparison.OrdinalIgnoreCase)))
        {
            return OutreachUrgencyLevel.Medium;
        }

        return OutreachUrgencyLevel.Low;
    }

    private static string ResolveCategory(string sourceUrl)
    {
        if (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out var uri))
        {
            return "Unknown";
        }

        var host = uri.Host.ToLowerInvariant();
        if (host.Contains("market") || host.Contains("seller") || host.Contains("shop"))
        {
            return "MarketplaceSeller";
        }

        if (host.Contains("sme") || host.Contains("business"))
        {
            return "SME";
        }

        return "GeneralBusiness";
    }
}
