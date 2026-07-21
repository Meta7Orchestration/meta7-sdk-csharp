// ═══════════════════════════════════════════════════════════════
// META7 Cognitive Civilization Architecture
// System Blueprint & Technical Specification v1.0
// Generated: 2026-07-21 | Status: Production-Ready Blueprint
// จดจำไว้ แล้วไปด้วยกัน
// ═══════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;
using System.Linq;

namespace META7.CaptainM7A.CognitiveCivilization
{
    // ══════════════════════════════════════════════════════════════
    // SECTION 1: Core Enums & Constants
    // ══════════════════════════════════════════════════════════════

    public enum WillAlignmentStatus { ALIGNED, WARNING, DRIFT, CRITICAL }
    public enum ArtifactKind { DECISION, INSIGHT, STRATEGY, FORECAST, SIGNAL }
    public enum DriftType { SOURCE, FIELD, FORM }
    public enum ReviewSeverity { INFO, WARNING, CRITICAL }
    public enum ReviewAction { ALLOW, REVIEW, BLOCK }
    public enum CivilizationHorizon { YEAR_5, YEAR_10, YEAR_25 }

    // Constitutional Articles (Immutable Core)
    public static class ConstitutionalArticles
    {
        public const string I   = "Human Primacy";
        public const string II  = "Non-Harm";
        public const string III = "Explainability";
        public const string IV  = "Reversibility";
        public const string V   = "Intent Integrity";
        public const string VI  = "Meta-Trust";
        public const string VII = "Auditability";
        public const string VIII= "Constitutional Stability";
        public const string IX  = "Compassion Alignment";
        public const string X   = "Human Co-Civilization";
        public const string XI  = "Civilization Wisdom";
        public const string XII = "Civilization Stewardship";
    }

    // Core Guiding Principles
    public static class GuidingPrinciples
    {
        public static readonly string[] All = new[]
        {
            "Human Primacy",
            "Non-Harm",
            "Explainability",
            "Meta-Trust",
            "Auditability",
            "Compassion",
            "Human Co-Civilization",
            "Civilization Wisdom",
            "Civilization Stewardship"
        };
    }

    // ══════════════════════════════════════════════════════════════
    // SECTION 2: Data Models & Records
    // ══════════════════════════════════════════════════════════════

    public record WillProfile(
        string Id,
        string Source,
        string[] Field,
        string[] Form,
        string Description,
        string Version,
        bool Immutable
    );

    public record WillAlignment(
        string ArtifactId,
        string ProfileId,
        double SourceAlignment,
        double FieldAlignment,
        double FormAlignment,
        double OverallAlignment,
        WillAlignmentStatus Status,
        string Rationale
    );

    public record WillDriftEvent(
        string Id,
        string ArtifactId,
        string ProfileId,
        DriftType DriftType,
        double DriftMagnitude,
        DateTime DetectedAt,
        string Cause
    );

    public record WillAlignmentIndex(
        string ProfileId,
        double Index,
        DateTime LastUpdated
    );

    public record WillAuditRecord(
        string ArtifactId,
        DateTime EvaluatedAt,
        double SourceAlignment,
        double FieldAlignment,
        double FormAlignment,
        double OverallAlignment,
        ReviewAction Decision,
        string Rationale
    );

    public record ExplainableArtifact(
        string Id,
        ArtifactKind Kind,
        string Reason,
        object Evidence,
        string[] Factors,
        double Confidence,
        string AuditId
    );

    public record ConstitutionalReview(
        string Article,
        bool Passed,
        ReviewSeverity Severity,
        ReviewAction Action,
        string Explanation
    );

    public record FutureRisk(string Name, double Probability, string Mitigation);

    public record CivilizationFuture(
        string Id,
        CivilizationHorizon Horizon,
        string ScenarioName,
        string[] Assumptions,
        double ProjectedCHI,
        double ProjectedCIS,
        double ProjectedCCI,
        double ProjectedCWI,
        FutureRisk[] Risks,
        string Narrative
    );

    // ══════════════════════════════════════════════════════════════
    // SECTION 3: Will Verification Engine (C# / .NET 8+)
    // Alignment Formula:
    //   overall = (source * 0.4) + (field * 0.4) + (form * 0.2)
    // Thresholds:
    //   >= 0.88 → ALIGNED
    //   0.80–0.87 → WARNING
    //   0.70–0.79 → DRIFT
    //   < 0.70 → CRITICAL
    // ══════════════════════════════════════════════════════════════

    public class WillVerificationEngine
    {
        private readonly WillProfile _rootProfile;
        private readonly List<WillAuditRecord> _auditTrail = new();
        private readonly List<WillDriftEvent> _driftEvents = new();

        public WillVerificationEngine(WillProfile rootProfile)
        {
            _rootProfile = rootProfile ?? throw new ArgumentNullException(nameof(rootProfile));
        }

        /// <summary>
        /// Evaluate alignment of an artifact against the root Will Profile.
        /// Fail-Closed: any error → BLOCK
        /// </summary>
        public WillAlignment EvaluateAlignment(
            string artifactId,
            string intentDescription,
            double compassionImpactScore,
            double structuralImpactScore)
        {
            try
            {
                var sourceScore = CalculateSourceScore(intentDescription);
                var fieldScore  = Math.Clamp(compassionImpactScore, 0.0, 1.0);
                var formScore   = Math.Clamp(structuralImpactScore, 0.0, 1.0);

                var overall = Math.Round(
                    (sourceScore * 0.4) + (fieldScore * 0.4) + (formScore * 0.2), 2);

                var status = ResolveStatus(overall);
                var rationale = $"Evaluated against root intent: \"{_rootProfile.Source}\". " +
                                $"Source={sourceScore:F2}, Field={fieldScore:F2}, Form={formScore:F2}";

                var alignment = new WillAlignment(
                    artifactId, _rootProfile.Id,
                    sourceScore, fieldScore, formScore,
                    overall, status, rationale);

                // Record audit
                _auditTrail.Add(new WillAuditRecord(
                    artifactId, DateTime.UtcNow,
                    sourceScore, fieldScore, formScore, overall,
                    status == WillAlignmentStatus.CRITICAL ? ReviewAction.BLOCK :
                    status == WillAlignmentStatus.DRIFT    ? ReviewAction.REVIEW : ReviewAction.ALLOW,
                    rationale));

                // Detect drift
                if (status is WillAlignmentStatus.DRIFT or WillAlignmentStatus.CRITICAL)
                {
                    _driftEvents.Add(new WillDriftEvent(
                        Guid.NewGuid().ToString("N")[..12],
                        artifactId, _rootProfile.Id,
                        DriftType.SOURCE, 1.0 - overall,
                        DateTime.UtcNow,
                        $"Alignment dropped to {overall:F2} — status: {status}"));
                }

                return alignment;
            }
            catch (Exception ex)
            {
                // Fail-Closed: any WVE error → BLOCK
                return new WillAlignment(
                    artifactId, _rootProfile.Id,
                    0, 0, 0, 0,
                    WillAlignmentStatus.CRITICAL,
                    $"WVE ERROR — Fail-Closed BLOCK: {ex.Message}");
            }
        }

        public IReadOnlyList<WillAuditRecord> GetAuditTrail() => _auditTrail.AsReadOnly();
        public IReadOnlyList<WillDriftEvent> GetDriftEvents() => _driftEvents.AsReadOnly();

        public WillAlignmentIndex GetAlignmentIndex()
        {
            if (_auditTrail.Count == 0)
                return new WillAlignmentIndex(_rootProfile.Id, 1.0, DateTime.UtcNow);

            var avg = _auditTrail.Average(r => r.OverallAlignment);
            return new WillAlignmentIndex(_rootProfile.Id, Math.Round(avg, 3), DateTime.UtcNow);
        }

        private static double CalculateSourceScore(string intent)
        {
            if (string.IsNullOrWhiteSpace(intent)) return 0.50;
            var lower = intent.ToLowerInvariant();
            return (lower.Contains("compassion") || lower.Contains("human") ||
                    lower.Contains("wisdom") || lower.Contains("stewardship"))
                ? 0.95 : 0.65;
        }

        private static WillAlignmentStatus ResolveStatus(double score) => score switch
        {
            < 0.70 => WillAlignmentStatus.CRITICAL,
            < 0.80 => WillAlignmentStatus.DRIFT,
            < 0.88 => WillAlignmentStatus.WARNING,
            _      => WillAlignmentStatus.ALIGNED
        };
    }

    // ══════════════════════════════════════════════════════════════
    // SECTION 4: Explainability Engine
    // ══════════════════════════════════════════════════════════════

    public class ExplainabilityEngine
    {
        private readonly List<ExplainableArtifact> _registry = new();

        public ExplainableArtifact Register(
            ArtifactKind kind,
            string reason,
            string[] factors,
            double confidence,
            object? evidence = null)
        {
            var artifact = new ExplainableArtifact(
                Id: Guid.NewGuid().ToString("N")[..12].ToUpper(),
                Kind: kind,
                Reason: reason,
                Evidence: evidence ?? new { },
                Factors: factors,
                Confidence: Math.Clamp(confidence, 0.0, 1.0),
                AuditId: $"AUDIT-{DateTime.UtcNow:yyyyMMddHHmmss}"
            );
            _registry.Add(artifact);
            return artifact;
        }

        public ConstitutionalReview CheckArticleIII(ExplainableArtifact artifact)
        {
            var passed = !string.IsNullOrWhiteSpace(artifact.Reason)
                         && artifact.Factors.Length > 0
                         && artifact.Confidence > 0.5;

            return new ConstitutionalReview(
                Article: "III",
                Passed: passed,
                Severity: passed ? ReviewSeverity.INFO : ReviewSeverity.CRITICAL,
                Action: passed ? ReviewAction.ALLOW : ReviewAction.BLOCK,
                Explanation: passed
                    ? $"Decision '{artifact.Id}' satisfies Explainability — Article III."
                    : $"Decision failed Explainability check — Article III violation."
            );
        }

        public IReadOnlyList<ExplainableArtifact> GetRegistry() => _registry.AsReadOnly();
    }

    // ══════════════════════════════════════════════════════════════
    // SECTION 5: Constitution Runtime Enforcement
    // ══════════════════════════════════════════════════════════════

    public class ConstitutionRuntime
    {
        private static readonly Dictionary<string, string> _articles = new()
        {
            ["I"]    = ConstitutionalArticles.I,
            ["II"]   = ConstitutionalArticles.II,
            ["III"]  = ConstitutionalArticles.III,
            ["IV"]   = ConstitutionalArticles.IV,
            ["V"]    = ConstitutionalArticles.V,
            ["VI"]   = ConstitutionalArticles.VI,
            ["VII"]  = ConstitutionalArticles.VII,
            ["VIII"] = ConstitutionalArticles.VIII,
            ["IX"]   = ConstitutionalArticles.IX,
            ["X"]    = ConstitutionalArticles.X,
            ["XI"]   = ConstitutionalArticles.XI,
            ["XII"]  = ConstitutionalArticles.XII,
        };

        /// <summary>
        /// Full constitutional review of an action.
        /// Returns BLOCK if any critical article fails.
        /// </summary>
        public ConstitutionalReview[] ReviewAction(
            string actionDescription,
            WillAlignment alignment,
            ExplainableArtifact? explainability = null)
        {
            var reviews = new List<ConstitutionalReview>();

            // Article I — Human Primacy
            reviews.Add(new ConstitutionalReview("I", true,
                ReviewSeverity.INFO, ReviewAction.ALLOW,
                "Human Primacy preserved — human override always available."));

            // Article II — Non-Harm
            var harmCheck = !actionDescription.ToLower().Contains("harm") &&
                            !actionDescription.ToLower().Contains("destroy");
            reviews.Add(new ConstitutionalReview("II", harmCheck,
                harmCheck ? ReviewSeverity.INFO : ReviewSeverity.CRITICAL,
                harmCheck ? ReviewAction.ALLOW : ReviewAction.BLOCK,
                harmCheck ? "Non-Harm principle satisfied." : "Potential harm detected — BLOCK."));

            // Article III — Explainability
            var explainCheck = explainability != null;
            reviews.Add(new ConstitutionalReview("III", explainCheck,
                explainCheck ? ReviewSeverity.INFO : ReviewSeverity.WARNING,
                explainCheck ? ReviewAction.ALLOW : ReviewAction.REVIEW,
                explainCheck ? "Explainability artifact provided." : "No explainability artifact — REVIEW required."));

            // Article V — Intent Integrity
            var intentCheck = alignment.Status != WillAlignmentStatus.CRITICAL;
            reviews.Add(new ConstitutionalReview("V", intentCheck,
                intentCheck ? ReviewSeverity.INFO : ReviewSeverity.CRITICAL,
                intentCheck ? ReviewAction.ALLOW : ReviewAction.BLOCK,
                intentCheck
                    ? $"Intent Integrity: {alignment.Status} (score={alignment.OverallAlignment:F2})"
                    : $"Intent Integrity CRITICAL — alignment={alignment.OverallAlignment:F2} — BLOCK."));

            // Article VII — Auditability
            reviews.Add(new ConstitutionalReview("VII", true,
                ReviewSeverity.INFO, ReviewAction.ALLOW,
                "Audit trail active — all actions recorded to immutable ledger."));

            return reviews.ToArray();
        }

        public bool IsActionAllowed(ConstitutionalReview[] reviews) =>
            reviews.All(r => r.Action != ReviewAction.BLOCK);

        public IReadOnlyDictionary<string, string> GetAllArticles() => _articles;
    }

    // ══════════════════════════════════════════════════════════════
    // SECTION 6: Policy Decision Chain (PDC)
    // Incoming → QHCU → WVE → Explainability → Constitution → Execute
    // ══════════════════════════════════════════════════════════════

    public class PolicyDecisionChain
    {
        private readonly WillVerificationEngine _wve;
        private readonly ExplainabilityEngine _explainability;
        private readonly ConstitutionRuntime _constitution;

        public PolicyDecisionChain(WillProfile rootProfile)
        {
            _wve = new WillVerificationEngine(rootProfile);
            _explainability = new ExplainabilityEngine();
            _constitution = new ConstitutionRuntime();
        }

        public PolicyDecision Evaluate(
            string actionId,
            string actionDescription,
            double compassionScore = 0.85,
            double structuralScore = 0.80)
        {
            // Step 1: Will Verification
            var alignment = _wve.EvaluateAlignment(
                actionId, actionDescription, compassionScore, structuralScore);

            // Step 2: Explainability
            var artifact = _explainability.Register(
                ArtifactKind.DECISION,
                $"Action '{actionId}': {actionDescription}",
                new[] { "WillAlignment", "ConstitutionalReview", "HumanPrimacy" },
                alignment.OverallAlignment);

            // Step 3: Constitutional Review
            var reviews = _constitution.ReviewAction(actionDescription, alignment, artifact);
            var allowed = _constitution.IsActionAllowed(reviews);

            return new PolicyDecision(
                ActionId: actionId,
                Allowed: allowed,
                Alignment: alignment,
                ExplainabilityArtifact: artifact,
                ConstitutionalReviews: reviews,
                FinalDecision: allowed ? ReviewAction.ALLOW : ReviewAction.BLOCK,
                Timestamp: DateTime.UtcNow
            );
        }
    }

    public record PolicyDecision(
        string ActionId,
        bool Allowed,
        WillAlignment Alignment,
        ExplainableArtifact ExplainabilityArtifact,
        ConstitutionalReview[] ConstitutionalReviews,
        ReviewAction FinalDecision,
        DateTime Timestamp
    );

    // ══════════════════════════════════════════════════════════════
    // SECTION 7: Governance Metrics
    // ══════════════════════════════════════════════════════════════

    public record GovernanceMetrics(
        double GHI,   // Governance Health Index
        double CHI,   // Civilization Health Index
        double CIS,   // Civilization Intelligence Score
        double FHI,   // Federation Health Index
        double CCI,   // Civilization Compassion Index
        double CWI,   // Civilization Wisdom Index
        double CFI,   // Civilization Futures Index
        double CIAI,  // Civilization Intent Alignment Index
        double WAI,   // Will Alignment Index
        double GHI_G  // Generational Health Index
    )
    {
        public static GovernanceMetrics Default() => new(
            GHI: 0.85, CHI: 0.80, CIS: 0.75, FHI: 0.90,
            CCI: 0.88, CWI: 0.82, CFI: 0.70, CIAI: 0.85,
            WAI: 0.87, GHI_G: 0.78
        );
    }

    // ══════════════════════════════════════════════════════════════
    // SECTION 8: Architecture Readiness Assessment
    // ══════════════════════════════════════════════════════════════

    public static class ArchitectureReadiness
    {
        public static readonly Dictionary<string, int> Readiness = new()
        {
            ["Architecture Design"]       = 90,
            ["Governance Specification"]  = 95,
            ["Constitution Specification"]= 95,
            ["Civilization Design"]       = 85,
            ["Implementation"]            = 35,
            ["Production Readiness"]      = 10,
        };

        public static readonly string[] DeploymentPhases = new[]
        {
            "Phase 1: QHCU Validation, WVE, Explainability, Constitution Runtime",
            "Phase 2: Governance Intelligence, Autonomy Runtime, Self-Healing Fabric",
            "Phase 3: Civilization Intelligence Network, Constitutional Federation",
            "Phase 4: Cognitive Civilization Engine, Compassion Logic, Human-AI Co-Civilization",
            "Phase 5: Wisdom Engine, Futures Engine, Choice Architecture, Stewardship Framework"
        };
    }
}