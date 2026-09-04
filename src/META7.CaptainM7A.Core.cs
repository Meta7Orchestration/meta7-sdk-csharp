// ══════════════════════════════════════════════════════════════════════════════
// META7 Captain M7A SDK — Core Types & Layers 1-15
// จดจำไว้ แล้วไปด้วยกัน
// ══════════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace META7.CaptainM7A
{
    // ── Enums ─────────────────────────────────────────────────────────────────

    public enum AgentRole    { COMMANDER, FORGE, SENTINEL, ECHO, SCHOLAR, NEXUS }
    public enum DirectiveType { ANALYZE, PLAN, EXECUTE, REPORT, HALT }
    public enum RiskLevel    { LOW = 0, MEDIUM = 1, HIGH = 2, CRITICAL = 3 }
    public enum SystemState  { NORMAL, SAFE_LOCK, RESYNC, DEGRADED, HALTED }
    public enum EventType
    {
        DirectiveIssued, LaunchExecuted, InvariantChecked,
        BarrierReleased, SafeLockActivated, SafeLockReleased,
        MeaningStoneRegistered, EpochAdvanced
    }

    // ── Core Records ──────────────────────────────────────────────────────────

    public record Directive(
        string Id,
        DirectiveType Type,
        string Payload,
        RiskLevel Risk,
        DateTime IssuedAt);

    public record BattlefieldMetrics
    {
        public double RequestsPerSecond { get; init; }
        public double LatencyP95Ms      { get; init; }
        public double CpuPercentage     { get; init; }
        public double MemoryPercentage  { get; init; }
    }

    // ── Layer 1: Core Types ───────────────────────────────────────────────────

    public record ThreatSignal(string Id, double Intensity, string Source, DateTime DetectedAt);

    public record CoherenceResult(double Score, bool Passed, string Rationale);

    public record WillForm(string Id, string Content, double CoherenceScore, DateTime FormedAt);

    // ── Layer 2: Strategic Intelligence ──────────────────────────────────────

    public class StrategicIntelligence
    {
        public ThreatSignal AnalyzeThreat(BattlefieldMetrics m)
        {
            var intensity = (m.RequestsPerSecond / 1000.0 * 0.4)
                          + (m.LatencyP95Ms / 1000.0 * 0.3)
                          + (m.CpuPercentage / 100.0 * 0.3);
            return new ThreatSignal(
                $"THREAT-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
                Math.Min(intensity, 1.0),
                "BattlefieldMetrics",
                DateTime.UtcNow);
        }

        public CoherenceResult CheckCoherence(ThreatSignal signal, double threshold = 0.75)
        {
            var score = 1.0 - signal.Intensity;
            return new CoherenceResult(score, score >= threshold,
                score >= threshold ? "Coherent — safe to proceed" : "Incoherent — hold position");
        }
    }

    // ── Layer 3: M7A Strategic Commander ─────────────────────────────────────

    public class M7AStrategicCommander
    {
        private readonly StrategicIntelligence _intel = new();
        private SystemState _state = SystemState.NORMAL;
        public SystemState State => _state;

        public (bool CanLaunch, string Reason) EvaluateLaunch(BattlefieldMetrics metrics)
        {
            if (_state == SystemState.SAFE_LOCK)
                return (false, "SAFE_LOCK active — all launches blocked");
            if (_state == SystemState.HALTED)
                return (false, "System HALTED");

            var threat = _intel.AnalyzeThreat(metrics);
            var coherence = _intel.CheckCoherence(threat);

            if (!coherence.Passed)
                return (false, $"Coherence check failed: {coherence.Score:F2}");
            if (metrics.CpuPercentage > 90)
                return (false, "CPU saturation — blast radius too high");

            return (true, $"Launch approved — coherence={coherence.Score:F2}");
        }

        public void ActivateSafeLock()  => _state = SystemState.SAFE_LOCK;
        public void ReleaseSafeLock()   => _state = SystemState.NORMAL;
        public void Halt()              => _state = SystemState.HALTED;
        public void Resync()            => _state = SystemState.RESYNC;
    }

    // ── Layer 4: Strategic Cognitive Loop ─────────────────────────────────────

    public record CognitiveLoopResult(
        string Phase,
        bool Success,
        double SignalStrength,
        double CoherenceScore,
        string Decision,
        WillForm? Output);

    public class StrategicCognitiveLoop
    {
        private readonly StrategicIntelligence _intel = new();

        public CognitiveLoopResult Run(string intent, double signalStrength)
        {
            // Synthesize
            var metrics = new BattlefieldMetrics
            {
                RequestsPerSecond = signalStrength * 1000,
                LatencyP95Ms      = (1 - signalStrength) * 500,
                CpuPercentage     = signalStrength * 60,
                MemoryPercentage  = 40
            };

            // Observe
            var threat = _intel.AnalyzeThreat(metrics);

            // Decide
            var coherence = _intel.CheckCoherence(threat, threshold: 0.25);
            var decision  = coherence.Passed ? "PROCEED" : "HOLD";

            // Act
            WillForm? form = null;
            if (decision == "PROCEED")
                form = new WillForm(
                    $"WF-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
                    $"Executed: {intent}",
                    coherence.Score,
                    DateTime.UtcNow);

            return new CognitiveLoopResult(
                "Synthesize→Decide→Act→Observe",
                decision == "PROCEED",
                signalStrength,
                coherence.Score,
                decision,
                form);
        }
    }

    // ── Layer 5: Command Pipeline & Gateway ───────────────────────────────────

    public enum GatewayVerdict { APPROVED, BLOCKED_RISK, BLOCKED_SAFELOCK, BLOCKED_BUDGET }

    public record GatewayResult(GatewayVerdict Verdict, string Reason, Directive? PassedDirective);

    public class CommandGateway
    {
        private bool _safeLockActive = false;
        private double _budgetUsed   = 0;
        private const double BudgetLimit = 90.0;

        public GatewayResult Evaluate(Directive directive, double costEstimate = 5.0)
        {
            if (_safeLockActive)
                return new GatewayResult(GatewayVerdict.BLOCKED_SAFELOCK,
                    "SAFE_LOCK active", null);

            if (directive.Risk == RiskLevel.CRITICAL)
                return new GatewayResult(GatewayVerdict.BLOCKED_RISK,
                    "CRITICAL risk directives require dual approval", null);

            if (_budgetUsed + costEstimate > BudgetLimit)
                return new GatewayResult(GatewayVerdict.BLOCKED_BUDGET,
                    $"Budget limit {BudgetLimit}% would be exceeded", null);

            _budgetUsed += costEstimate;
            return new GatewayResult(GatewayVerdict.APPROVED,
                $"Approved — budget used: {_budgetUsed:F1}%", directive);
        }

        public void ActivateSafeLock() => _safeLockActive = true;
        public void ReleaseSafeLock()  => _safeLockActive = false;
        public double BudgetUsed       => _budgetUsed;
    }

    // ── Layer 6: Barrier Network MB-SYNC-001 ──────────────────────────────────

    public enum BarrierState { OPEN, LOCKED, RELEASED }

    public class BarrierNetwork
    {
        private readonly HashSet<AgentRole> _required;
        private readonly HashSet<AgentRole> _checkedIn = new();
        private BarrierState _state = BarrierState.OPEN;
        private int _epoch = 1;
        private int _fallbackCount = 0;
        private const int MaxFallbacks = 6;

        public BarrierNetwork(IEnumerable<AgentRole> requiredAgents)
            => _required = new HashSet<AgentRole>(requiredAgents);

        public BarrierState State    => _state;
        public int Epoch             => _epoch;
        public int FallbackCount     => _fallbackCount;
        public int CheckedInCount    => _checkedIn.Count;

        public bool CheckIn(AgentRole agent)
        {
            if (_state == BarrierState.LOCKED) return false;
            _checkedIn.Add(agent);
            if (_checkedIn.IsSupersetOf(_required))
                _state = BarrierState.LOCKED;
            return true;
        }

        public bool Release()
        {
            if (_state != BarrierState.LOCKED) return false;
            _state = BarrierState.RELEASED;
            _epoch++;
            _checkedIn.Clear();
            return true;
        }

        public void RecordFallback()
        {
            _fallbackCount++;
            if (_fallbackCount >= MaxFallbacks)
                _state = BarrierState.LOCKED; // degrade
        }

        public bool AllCheckedIn => _checkedIn.IsSupersetOf(_required);
    }

    // ── Layer 7: Audit Ledger + Replay Engine ─────────────────────────────────

    public record AuditEntry(
        long Sequence,
        EventType Type,
        string ActorId,
        string Payload,
        string Hash,
        DateTime Timestamp);

    public class AuditLedger
    {
        private readonly List<AuditEntry> _entries = new();
        private long _sequence = 0;
        private string _lastHash = "GENESIS";

        public AuditEntry Append(EventType type, string actorId, string payload)
        {
            _sequence++;
            var raw  = $"{_sequence}|{type}|{actorId}|{payload}|{_lastHash}";
            var hash = ComputeHash(raw);
            var entry = new AuditEntry(_sequence, type, actorId, payload, hash, DateTime.UtcNow);
            _entries.Add(entry);
            _lastHash = hash;
            return entry;
        }

        public bool VerifyChain()
        {
            var prevHash = "GENESIS";
            foreach (var e in _entries)
            {
                var raw      = $"{e.Sequence}|{e.Type}|{e.ActorId}|{e.Payload}|{prevHash}";
                var expected = ComputeHash(raw);
                if (expected != e.Hash) return false;
                prevHash = e.Hash;
            }
            return true;
        }

        public IReadOnlyList<AuditEntry> Entries => _entries.AsReadOnly();
        public long LastSequence => _sequence;

        private static string ComputeHash(string input)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes)[..16];
        }
    }

    // ── Layer 8: Sovereign Control Plane ──────────────────────────────────────

    public enum SovereignMode { NORMAL, SAFE_LOCK, EMERGENCY }

    public class SovereignControlPlane
    {
        private SovereignMode _mode = SovereignMode.NORMAL;
        private readonly AuditLedger _ledger = new();
        private readonly List<string> _commandLog = new();

        public SovereignMode Mode => _mode;

        public bool IssueCommand(string commandId, string payload, AgentRole issuer)
        {
            if (_mode == SovereignMode.SAFE_LOCK)
            {
                _ledger.Append(EventType.SafeLockActivated, issuer.ToString(),
                    $"BLOCKED:{commandId}");
                return false;
            }
            _commandLog.Add($"{commandId}:{payload}");
            _ledger.Append(EventType.DirectiveIssued, issuer.ToString(), commandId);
            return true;
        }

        public void ActivateSafeLock(string reason)
        {
            _mode = SovereignMode.SAFE_LOCK;
            _ledger.Append(EventType.SafeLockActivated, "SOVEREIGN", reason);
        }

        public void ReleaseSafeLock()
        {
            _mode = SovereignMode.NORMAL;
            _ledger.Append(EventType.SafeLockReleased, "SOVEREIGN", "Manual release");
        }

        public AuditLedger Ledger => _ledger;
        public IReadOnlyList<string> CommandLog => _commandLog.AsReadOnly();
    }

    // ── Layer 9: Strategic Doctrine ───────────────────────────────────────────

    public record DoctrineRule(string Name, string Invariant, string Action);

    public class StrategicDoctrine
    {
        public static readonly IReadOnlyList<DoctrineRule> V1Rules = new List<DoctrineRule>
        {
            new("NoStaleAction",         "State must be current",          "BLOCK if stale"),
            new("NoDuplicateExecution",  "Each directive runs once",       "BLOCK if duplicate"),
            new("DeterministicOrdering", "Events ordered by sequence",     "ENFORCE sequence"),
            new("SafeLockSupremacy",     "SafeLock overrides all",         "HALT on SafeLock"),
            new("BlastRadiusLimit",      "CPU < 90%, budget < 90%",        "BLOCK if exceeded"),
        };

        public static readonly IReadOnlyList<DoctrineRule> V2Rules = new List<DoctrineRule>(V1Rules)
        {
            new("CoherenceMinimum",      "Coherence >= 0.75",              "HOLD if below"),
            new("HumanGateRequired",     "CRITICAL needs dual approval",   "ESCALATE"),
            new("AuditTrailMandatory",   "All actions logged immutably",   "ENFORCE hash chain"),
        };

        public bool Validate(Directive d, SystemState state, double coherence)
        {
            if (state == SystemState.SAFE_LOCK) return false;
            if (d.Risk == RiskLevel.CRITICAL)   return false;
            if (coherence < 0.25)               return false;
            return true;
        }
    }

    // ── Layer 10: Command Simulation Engine ───────────────────────────────────

    public record SimulationScenario(string Name, BattlefieldMetrics Metrics, bool ExpectLaunch);

    public record SimulationResult(
        string ScenarioName,
        bool LaunchDecision,
        bool ExpectedLaunch,
        bool Passed,
        string Reason);

    public class CommandSimulationEngine
    {
        private readonly M7AStrategicCommander _commander = new();

        public SimulationResult Run(SimulationScenario scenario)
        {
            var (canLaunch, reason) = _commander.EvaluateLaunch(scenario.Metrics);
            return new SimulationResult(
                scenario.Name,
                canLaunch,
                scenario.ExpectLaunch,
                canLaunch == scenario.ExpectLaunch,
                reason);
        }

        public static IReadOnlyList<SimulationScenario> StandardScenarios => new List<SimulationScenario>
        {
            // intensity = (RPS/1000*0.4) + (Lat/1000*0.3) + (CPU/100*0.3); score = 1 - intensity; pass if score >= 0.75
            new("Normal Operations",
                new BattlefieldMetrics { RequestsPerSecond=100, LatencyP95Ms=50, CpuPercentage=20, MemoryPercentage=30 },
                true),   // intensity=0.04+0.015+0.06=0.115 → score=0.885 ✓
            new("CPU Saturation",
                new BattlefieldMetrics { RequestsPerSecond=900, LatencyP95Ms=800, CpuPercentage=95, MemoryPercentage=80 },
                false),  // CPU>90 → blocked ✓
            new("High Traffic — Coherent",
                new BattlefieldMetrics { RequestsPerSecond=150, LatencyP95Ms=80, CpuPercentage=25, MemoryPercentage=40 },
                true),   // intensity=0.06+0.024+0.075=0.159 → score=0.841 ✓
            new("Extreme Latency",
                new BattlefieldMetrics { RequestsPerSecond=100, LatencyP95Ms=950, CpuPercentage=30, MemoryPercentage=40 },
                false),  // intensity=0.04+0.285+0.09=0.415 → score=0.585 < 0.75 ✓
            new("Balanced Load",
                new BattlefieldMetrics { RequestsPerSecond=120, LatencyP95Ms=60, CpuPercentage=22, MemoryPercentage=35 },
                true),   // intensity=0.048+0.018+0.066=0.132 → score=0.868 ✓
        };
    }

    // ── Layer 11: Canonical Event Contract ────────────────────────────────────

    public record CanonicalEvent(
        string EventId,
        long Sequence,
        int Epoch,
        EventType Type,
        string ActorId,
        string Payload,
        string PreviousHash,
        string Hash,
        DateTime Timestamp);

    public static class CanonicalEventFactory
    {
        public static CanonicalEvent Create(long seq, int epoch, EventType type,
            string actorId, string payload, string prevHash)
        {
            var id  = $"EVT-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
            var raw = $"{seq}|{epoch}|{type}|{actorId}|{payload}|{prevHash}";
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)))[..16];
            return new CanonicalEvent(id, seq, epoch, type, actorId, payload, prevHash, hash, DateTime.UtcNow);
        }

        public static bool VerifyHash(CanonicalEvent e)
        {
            var raw      = $"{e.Sequence}|{e.Epoch}|{e.Type}|{e.ActorId}|{e.Payload}|{e.PreviousHash}";
            var expected = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)))[..16];
            return expected == e.Hash;
        }
    }

    // ── Layer 12: Invariant Engine ────────────────────────────────────────────

    public enum InvariantStatus { PASS, FAIL }

    public record InvariantResult(string Name, InvariantStatus Status, string Detail);

    public class InvariantEngine
    {
        public InvariantResult CheckProjectionAtomicity(long stateVersion, long completedEvents)
            => stateVersion == completedEvents
                ? new("ProjectionAtomicity", InvariantStatus.PASS, $"version={stateVersion} == events={completedEvents}")
                : new("ProjectionAtomicity", InvariantStatus.FAIL, $"version={stateVersion} != events={completedEvents}");

        public InvariantResult CheckNoDuplicates(IEnumerable<string> eventIds)
        {
            var ids  = eventIds.ToList();
            var dups = ids.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            return dups.Count == 0
                ? new("NoDuplicateExecution", InvariantStatus.PASS, "No duplicates found")
                : new("NoDuplicateExecution", InvariantStatus.FAIL, $"Duplicates: {string.Join(",", dups)}");
        }

        public InvariantResult CheckHashChain(IEnumerable<CanonicalEvent> events)
        {
            var list     = events.ToList();
            var prevHash = "GENESIS";
            foreach (var e in list)
            {
                if (e.PreviousHash != prevHash)
                    return new("HashChainIntegrity", InvariantStatus.FAIL,
                        $"Chain broken at seq={e.Sequence}");
                if (!CanonicalEventFactory.VerifyHash(e))
                    return new("HashChainIntegrity", InvariantStatus.FAIL,
                        $"Hash mismatch at seq={e.Sequence}");
                prevHash = e.Hash;
            }
            return new("HashChainIntegrity", InvariantStatus.PASS, $"Chain intact ({list.Count} events)");
        }

        public InvariantResult CheckSequenceConsistency(IEnumerable<CanonicalEvent> events)
        {
            var list = events.OrderBy(e => e.Sequence).ToList();
            for (int i = 0; i < list.Count; i++)
                if (list[i].Sequence != i + 1)
                    return new("SequenceConsistency", InvariantStatus.FAIL,
                        $"Gap at position {i}: expected {i+1}, got {list[i].Sequence}");
            return new("SequenceConsistency", InvariantStatus.PASS, $"Sequence consistent ({list.Count} events)");
        }

        public InvariantResult CheckBarrierConsistency(BarrierState state, bool allCheckedIn)
        {
            if (state == BarrierState.LOCKED && !allCheckedIn)
                return new("BarrierConsistency", InvariantStatus.FAIL,
                    "Barrier LOCKED but not all agents checked in");
            return new("BarrierConsistency", InvariantStatus.PASS, $"Barrier state={state} consistent");
        }

        public InvariantResult CheckActionSafety(SystemState sysState, RiskLevel risk)
        {
            if (sysState == SystemState.SAFE_LOCK)
                return new("ActionSafety", InvariantStatus.FAIL, "SAFE_LOCK blocks all actions");
            if (risk == RiskLevel.CRITICAL && sysState != SystemState.NORMAL)
                return new("ActionSafety", InvariantStatus.FAIL, "CRITICAL risk in non-NORMAL state");
            return new("ActionSafety", InvariantStatus.PASS, $"Action safe: state={sysState}, risk={risk}");
        }
    }

    // ── Layer 13: Meaning Stone ───────────────────────────────────────────────

    public record MeaningStone(
        string StoneId,
        string Origin,
        string MeaningCore,
        string Artifact,
        IReadOnlyList<string> Evidence,
        int Version,
        string? ParentStoneId,
        DateTime CreatedAt);

    public class MeaningStoneRepository
    {
        private readonly Dictionary<string, MeaningStone> _stones = new();

        public MeaningStone Create(string origin, string meaningCore, string artifact,
            IEnumerable<string> evidence)
        {
            var stone = new MeaningStone(
                $"MS-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
                origin, meaningCore, artifact,
                evidence.ToList().AsReadOnly(),
                1, null, DateTime.UtcNow);
            _stones[stone.StoneId] = stone;
            return stone;
        }

        public MeaningStone Fork(string parentId, string newMeaningCore, string newArtifact)
        {
            if (!_stones.TryGetValue(parentId, out var parent))
                throw new KeyNotFoundException($"Stone {parentId} not found");

            var fork = new MeaningStone(
                $"MS-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
                parent.Origin, newMeaningCore, newArtifact,
                parent.Evidence,
                parent.Version + 1, parentId, DateTime.UtcNow);
            _stones[fork.StoneId] = fork;
            return fork;
        }

        public MeaningStone? Get(string id) => _stones.TryGetValue(id, out var s) ? s : null;
        public int Count => _stones.Count;
    }

    // ── Layer 14: Canonical Event Store ──────────────────────────────────────

    public class CanonicalEventStore
    {
        private readonly List<CanonicalEvent> _events = new();
        private long _sequence = 0;
        private int  _epoch    = 1;
        private string _lastHash = "GENESIS";
        private bool _safeLockActive = false;
        private readonly MeaningStoneRepository _stoneRepo = new();

        private static readonly HashSet<EventType> _safeLockBlocked = new()
        {
            EventType.LaunchExecuted, EventType.DirectiveIssued, EventType.EpochAdvanced
        };

        public CanonicalEvent Append(EventType type, string actorId, string payload)
        {
            if (_safeLockActive && _safeLockBlocked.Contains(type))
                throw new InvalidOperationException($"SAFE_LOCK blocks {type}");

            _sequence++;
            var evt = CanonicalEventFactory.Create(_sequence, _epoch, type, actorId, payload, _lastHash);
            _events.Add(evt);
            _lastHash = evt.Hash;
            return evt;
        }

        public void ActivateSafeLock()
        {
            _safeLockActive = true;
            Append(EventType.SafeLockActivated, "STORE", "SafeLock activated");
        }

        public void ReleaseSafeLock()
        {
            _safeLockActive = false;
            Append(EventType.SafeLockReleased, "STORE", "SafeLock released");
        }

        public MeaningStone RegisterMeaningStone(string origin, string core, string artifact,
            IEnumerable<string> evidence)
        {
            var stone = _stoneRepo.Create(origin, core, artifact, evidence);
            Append(EventType.MeaningStoneRegistered, "STORE", stone.StoneId);
            return stone;
        }

        public bool VerifyChain()
        {
            var prevHash = "GENESIS";
            foreach (var e in _events)
            {
                if (e.PreviousHash != prevHash) return false;
                if (!CanonicalEventFactory.VerifyHash(e)) return false;
                prevHash = e.Hash;
            }
            return true;
        }

        public IReadOnlyList<CanonicalEvent> Events => _events.AsReadOnly();
        public long LastSequence => _sequence;
        public bool SafeLockActive => _safeLockActive;
        public MeaningStoneRepository Stones => _stoneRepo;
    }

    // ── Layer 15: Will-Source → Meaning Stone Pipeline ────────────────────────

    public record WillSourceIntent(string Id, string Intent, double SignalStrength, DateTime CreatedAt);

    public record WillFieldExpansion(string IntentId, IReadOnlyList<string> Possibilities, double FieldStrength);

    public class WillSourcePipeline
    {
        private readonly StrategicCognitiveLoop _loop = new();
        private readonly MeaningStoneRepository _repo  = new();
        private readonly InvariantEngine _invariants    = new();

        public (MeaningStone? Stone, double Coherence, bool Success) Execute(WillSourceIntent intent)
        {
            // Will-Field: expand possibilities
            var possibilities = new List<string>
            {
                $"Path A: Direct execution of '{intent.Intent}'",
                $"Path B: Staged rollout of '{intent.Intent}'",
                $"Path C: Simulation-first for '{intent.Intent}'"
            };

            // Coherence check
            var loopResult = _loop.Run(intent.Intent, intent.SignalStrength);
            if (!loopResult.Success)
                return (null, loopResult.CoherenceScore, false);

            // Will-Form → Meaning Stone
            var evidence = possibilities.Take(2).ToList();
            var stone = _repo.Create(
                intent.Id,
                $"Validated intent: {intent.Intent}",
                loopResult.Output?.Content ?? "No output",
                evidence);

            return (stone, loopResult.CoherenceScore, true);
        }
    }
}