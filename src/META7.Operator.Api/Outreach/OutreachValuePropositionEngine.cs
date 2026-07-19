using System.Collections.Generic;
using System.Linq;
using META7.Operator.Contracts.Outreach;

namespace META7.Operator.Api.Outreach;

public sealed class OutreachValuePropositionEngine
{
    private static readonly IReadOnlyDictionary<string, string> ValueByPainPoint =
        new Dictionary<string, string>
        {
            ["NeedSupport"] = "ระบบแจ้งเตือนออเดอร์ทันที",
            ["OperationalAlert"] = "ระบบแจ้งเตือนออเดอร์ทันที",
            ["SystemOutage"] = "ระบบป้องกันออเดอร์ตกหล่น",
            ["SlowPOS"] = "ระบบแจ้งเตือนออเดอร์ทันที",
            ["DroppedOrders"] = "ระบบป้องกันออเดอร์ตกหล่น",
            ["UnclassifiedPainPoint"] = "ระบบตรวจจับสต็อกหมด"
        };

    public IReadOnlyList<string> Generate(OutreachContext context)
    {
        var values = context.PainPoints
            .Select(painPoint => ValueByPainPoint.TryGetValue(painPoint, out var proposition)
                ? proposition
                : "ระบบตรวจจับสต็อกหมด")
            .Distinct()
            .ToList();

        if (values.Count == 0)
        {
            values.Add("ระบบแจ้งเตือนออเดอร์ทันที");
        }

        return values;
    }
}
