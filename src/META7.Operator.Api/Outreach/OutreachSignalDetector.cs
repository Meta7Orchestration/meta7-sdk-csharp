using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using META7.Operator.Contracts.Outreach;

namespace META7.Operator.Api.Outreach;

public sealed class OutreachSignalDetector
{
    private static readonly string[] SignalKeywords =
    [
        "need help",
        "แจ้งเตือน",
        "ระบบล่ม",
        "POS ช้า",
        "ออเดอร์ตกหล่น"
    ];

    private static readonly IReadOnlyList<ReadOnlyBrowserActionType> ReadOnlyScanActions =
    [
        ReadOnlyBrowserActionType.Navigate,
        ReadOnlyBrowserActionType.ExtractText,
        ReadOnlyBrowserActionType.ExtractMetadata,
        ReadOnlyBrowserActionType.ExtractStructure
    ];

    private static readonly IReadOnlyDictionary<string, double> ConfidenceByKeyword =
        new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            ["need help"] = 0.62d,
            ["แจ้งเตือน"] = 0.71d,
            ["ระบบล่ม"] = 0.96d,
            ["POS ช้า"] = 0.85d,
            ["ออเดอร์ตกหล่น"] = 0.92d
        };

    private readonly IPlaywrightOperatorExecutor _playwrightExecutor;
    private readonly HashSet<string> _allowedDomains;

    public OutreachSignalDetector(
        IPlaywrightOperatorExecutor playwrightExecutor,
        IEnumerable<string> allowedDomains)
    {
        _playwrightExecutor = playwrightExecutor ?? throw new ArgumentNullException(nameof(playwrightExecutor));
        _allowedDomains = new HashSet<string>(
            (allowedDomains ?? throw new ArgumentNullException(nameof(allowedDomains)))
            .Select(d => d.Trim().ToLowerInvariant())
            .Where(d => !string.IsNullOrWhiteSpace(d)),
            StringComparer.OrdinalIgnoreCase);

        if (_allowedDomains.Count == 0)
        {
            throw new ArgumentException("At least one domain must be allowlisted.", nameof(allowedDomains));
        }
    }

    public async Task<IReadOnlyList<OutreachSignal>> DetectAsync(string targetUrl, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(targetUrl, UriKind.Absolute, out var targetUri))
        {
            throw new ArgumentException("Target URL must be absolute.", nameof(targetUrl));
        }

        if (!IsAllowedDomain(targetUri))
        {
            throw new InvalidOperationException($"Domain '{targetUri.Host}' is not in outreach allowlist.");
        }

        var snapshot = await _playwrightExecutor
            .CaptureReadOnlyAsync(targetUri, ReadOnlyScanActions, cancellationToken)
            .ConfigureAwait(false);

        var corpus = BuildCorpus(snapshot);
        var signals = new List<OutreachSignal>();

        foreach (var keyword in SignalKeywords)
        {
            foreach (var matchIndex in GetAllMatchIndexes(corpus, keyword))
            {
                signals.Add(new OutreachSignal(
                    SourceUrl: targetUri.ToString(),
                    MatchedKeyword: keyword,
                    MatchedText: CreateSnippet(corpus, matchIndex, keyword.Length),
                    SignalType: "OperationalPainPoint",
                    Confidence: ConfidenceByKeyword[keyword],
                    Metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["Host"] = targetUri.Host,
                        ["ReadOnlyAction"] = string.Join(",", ReadOnlyScanActions),
                        ["CueCount"] = snapshot.StructuralCues.Count.ToString()
                    }));
            }
        }

        return signals;
    }

    public bool IsAllowedDomain(Uri targetUri)
    {
        var host = targetUri.Host.ToLowerInvariant();
        return _allowedDomains.Any(domain => host.Equals(domain, StringComparison.OrdinalIgnoreCase)
            || host.EndsWith($".{domain}", StringComparison.OrdinalIgnoreCase));
    }

    private static string BuildCorpus(ReadOnlyPageSnapshot snapshot)
    {
        var parts = new List<string>
        {
            snapshot.BodyText ?? string.Empty
        };

        parts.AddRange(snapshot.Metadata.Select(m => $"{m.Key}:{m.Value}"));
        parts.AddRange(snapshot.StructuralCues);
        return string.Join('\n', parts);
    }

    private static IEnumerable<int> GetAllMatchIndexes(string input, string needle)
    {
        if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(needle))
        {
            yield break;
        }

        var cursor = 0;
        while (cursor < input.Length)
        {
            var index = input.IndexOf(needle, cursor, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                yield break;
            }

            yield return index;
            cursor = index + needle.Length;
        }
    }

    private static string CreateSnippet(string corpus, int matchIndex, int keywordLength)
    {
        const int sideWindow = 24;
        var start = Math.Max(0, matchIndex - sideWindow);
        var length = Math.Min(corpus.Length - start, keywordLength + sideWindow * 2);
        return corpus.Substring(start, length).Replace('\n', ' ').Trim();
    }
}
