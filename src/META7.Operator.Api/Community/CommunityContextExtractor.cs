// ══════════════════════════════════════════════════════════════════════════════
// META7 Operator — Community Connector Layer
// CommunityContextExtractor: Converts raw scan data into structured CommunityContext
// Does NOT generate outreach messages
// ══════════════════════════════════════════════════════════════════════════════

using META7.Operator.Api.Community.Models;
using META7.Operator.Contracts.Community;

namespace META7.Operator.Api.Community;

/// <summary>
/// Converts raw community post and metadata data into a structured <see cref="CommunityContext"/>.
/// Identifies pain points, recurring issues, trending topics, and urgency signals.
/// Does NOT generate any outreach messages or write content.
/// </summary>
public sealed class CommunityContextExtractor
{
    // Keywords used for heuristic signal detection (all lowercase)
    private static readonly string[] PainPointKeywords =
        ["problem", "issue", "broken", "not working", "error", "failed", "frustrat", "annoying", "worse", "terrible", "bad experience", "disappointed"];

    private static readonly string[] UrgencyKeywords =
        ["urgent", "asap", "emergency", "immediately", "help!", "please help", "critical", "deadline", "stuck", "desperate", "anyone?", "unanswered"];

    private static readonly string[] RecurringIssueThreshold =
        ["again", "still", "every time", "keeps", "always", "repeatedly", "same problem", "same issue"];

    /// <summary>
    /// Extracts a structured <see cref="CommunityContext"/> from <paramref name="metadata"/>
    /// and a list of <paramref name="posts"/>.
    /// </summary>
    public CommunityContext Extract(
        CommunityMetadata metadata,
        IReadOnlyList<CommunityPost> posts)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(posts);

        var signals = ExtractSignals(posts);

        return new CommunityContext
        {
            CommunityUrl   = metadata.Url,
            CommunityType  = metadata.Type.ToString(),
            ActivityLevel  = ClassifyActivity(metadata.ContentDensity, posts.Count),
            PainPoints     = signals
                .Where(s => s.Category == SignalCategory.PainPoint)
                .Select(s => s.Label)
                .ToList(),
            RecurringIssues = signals
                .Where(s => s.Category == SignalCategory.RecurringIssue)
                .Select(s => s.Label)
                .ToList(),
            TrendingTopics  = signals
                .Where(s => s.Category == SignalCategory.TrendingTopic)
                .Select(s => s.Label)
                .ToList(),
            UrgencySignals  = signals
                .Where(s => s.Category == SignalCategory.UrgencySignal)
                .Select(s => s.Label)
                .ToList(),
            ExtractedAt = DateTime.UtcNow
        };
    }

    private const int MaxExcerptLength = 60;

    /// <summary>
    /// Extracts analytical signals from a list of posts.
    /// </summary>
    public IReadOnlyList<CommunitySignal> ExtractSignals(IReadOnlyList<CommunityPost> posts)
    {
        ArgumentNullException.ThrowIfNull(posts);

        var signals = new List<CommunitySignal>();
        var topicFrequency = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var painOccurrences = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var recurringOccurrences = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var urgencyOccurrences = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var post in posts)
        {
            var content = post.Content.ToLowerInvariant();

            // Pain point detection
            foreach (var keyword in PainPointKeywords)
            {
                if (content.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    painOccurrences[keyword] = painOccurrences.GetValueOrDefault(keyword) + 1;
            }

            // Recurring issue detection
            foreach (var keyword in RecurringIssueThreshold)
            {
                if (content.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    recurringOccurrences[keyword] = recurringOccurrences.GetValueOrDefault(keyword) + 1;
            }

            // Urgency detection
            foreach (var keyword in UrgencyKeywords)
            {
                if (content.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    urgencyOccurrences[keyword] = urgencyOccurrences.GetValueOrDefault(keyword) + 1;
            }

            // Topic frequency: high-reaction posts are trending topics
            if (post.ReactionCount >= 5)
            {
                var excerpt = post.Content.Length > MaxExcerptLength ? post.Content[..MaxExcerptLength] + "…" : post.Content;
                topicFrequency[excerpt] = topicFrequency.GetValueOrDefault(excerpt) + post.ReactionCount;
            }
        }

        // Build pain-point signals
        foreach (var (keyword, count) in painOccurrences.Where(kv => kv.Value >= 1))
        {
            signals.Add(new CommunitySignal
            {
                SignalId    = $"PP-{keyword.Replace(" ", "-")}-{count}",
                Category    = SignalCategory.PainPoint,
                Label       = keyword,
                Occurrences = count,
                Confidence  = Math.Min(count / (double)Math.Max(posts.Count, 1), 1.0),
                DetectedAt  = DateTime.UtcNow
            });
        }

        // Build recurring-issue signals (require ≥2 occurrences)
        foreach (var (keyword, count) in recurringOccurrences.Where(kv => kv.Value >= 2))
        {
            signals.Add(new CommunitySignal
            {
                SignalId    = $"RI-{keyword.Replace(" ", "-")}-{count}",
                Category    = SignalCategory.RecurringIssue,
                Label       = keyword,
                Occurrences = count,
                Confidence  = Math.Min(count / (double)Math.Max(posts.Count, 1), 1.0),
                DetectedAt  = DateTime.UtcNow
            });
        }

        // Build urgency signals
        foreach (var (keyword, count) in urgencyOccurrences.Where(kv => kv.Value >= 1))
        {
            signals.Add(new CommunitySignal
            {
                SignalId    = $"US-{keyword.Replace(" ", "-")}-{count}",
                Category    = SignalCategory.UrgencySignal,
                Label       = keyword,
                Occurrences = count,
                Confidence  = Math.Min(count / (double)Math.Max(posts.Count, 1), 1.0),
                DetectedAt  = DateTime.UtcNow
            });
        }

        // Build trending-topic signals (top 5 by reaction count)
        foreach (var (excerpt, reactions) in topicFrequency.OrderByDescending(kv => kv.Value).Take(5))
        {
            signals.Add(new CommunitySignal
            {
                SignalId    = $"TT-{Math.Abs(excerpt.GetHashCode()):X8}",
                Category    = SignalCategory.TrendingTopic,
                Label       = excerpt,
                Occurrences = 1,
                Confidence  = Math.Min(reactions / 100.0, 1.0),
                Excerpt     = excerpt,
                DetectedAt  = DateTime.UtcNow
            });
        }

        return signals;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static ActivityLevel ClassifyActivity(double contentDensity, int postCount)
    {
        var combined = contentDensity + postCount;
        return combined switch
        {
            >= 50 => ActivityLevel.VeryHigh,
            >= 20 => ActivityLevel.High,
            >= 5  => ActivityLevel.Moderate,
            _     => ActivityLevel.Low
        };
    }
}
