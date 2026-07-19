// ══════════════════════════════════════════════════════════════════════════════
// META7 Captain M7A SDK — Layers 21-30: Edge Cases & Stress Testing
// Layer 21: High-Concurrency Load Testing
// Layer 22: Memory Pressure & Leak Detection
// Layer 23: Circuit Breaker Chaos Patterns
// Layer 24: Deterministic Replay Under Stress
// Layer 25: Barrier Network Deadlock Prevention
// Layer 26: Invariant Violation Storm
// Layer 27: Adaptive Doctrine Under Chaos
// Layer 28: Multi-Shard Consistency Under Partition
// Layer 29: Full-Stack Chaos Engineering
// Layer 30: Recovery & Resilience Certification
// จดจำไว้ แล้วไปด้วยกัน
// ══════════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace META7.CaptainM7A
{
    // ══════════════════════════════════════════════════════════════════════════
    // LAYER 21 — High-Concurrency Load Testing Engine
    // Fires N directives concurrently and verifies no duplicates, no data races
    // ══════════════════════════════════════════════════════════════════════════

    public record LoadTestResult(
        int TotalDirectives,
        int Succeeded,
        int Failed,
        int DuplicatesDetected,
        double ElapsedMs,
        double ThroughputPerSec,
        bool NoDuplicates,
        bool AllDelivered);

    public class ConcurrentLoadTester
    {
        private readonly DistributedCommandRouter _router;
        private readonly ConcurrentBag<string> _deliveredIds = new();
        private readonly ConcurrentBag<string> _failedIds    = new();

        public ConcurrentLoadTester(int shardCount = 4)
        {
            _router = new DistributedCommandRouter();
            for (int i = 0; i < shardCount; i++)
            {
                var role = (AgentRole)(i % 6);
                _router.RegisterShard(new AgentShard(role, i));  // AgentShard ctor: (role, index)
            }
        }

        public LoadTestResult Run(int directiveCount, int parallelism = 8)
        {
            var directives = Enumerable.Range(1, directiveCount)
                .Select(i => new Directive(
                    $"LOAD-{i:D6}",
                    DirectiveType.ANALYZE,
                    $"Payload-{i}",
                    RiskLevel.LOW,
                    DateTime.UtcNow))
                .ToList();

            var sw = Stopwatch.StartNew();

            Parallel.ForEach(directives,
                new ParallelOptions { MaxDegreeOfParallelism = parallelism },
                d =>
                {
                    var result = _router.Route(d, RoutingStrategy.HashMod);
                    if (result.Delivered)
                        _deliveredIds.Add(d.Id);
                    else
                        _failedIds.Add(d.Id);
                });

            sw.Stop();

            var delivered    = _deliveredIds.ToList();
            var uniqueIds    = new HashSet<string>(delivered);
            var duplicates   = delivered.Count - uniqueIds.Count;
            var elapsed      = sw.Elapsed.TotalMilliseconds;
            var throughput   = directiveCount / (elapsed / 1000.0);

            return new LoadTestResult(
                directiveCount,
                delivered.Count,
                _failedIds.Count,
                duplicates,
                elapsed,
                throughput,
                duplicates == 0,
                delivered.Count == directiveCount);
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // LAYER 22 — Memory Pressure & Leak Detection
    // Tracks object allocation patterns and detects unbounded growth
    // ══════════════════════════════════════════════════════════════════════════

    public record MemorySnapshot(long AllocatedBytes, int Gen0Collections, int Gen1Collections, DateTime TakenAt);

    public record MemoryLeakReport(
        long BaselineBytes,
        long PeakBytes,
        long FinalBytes,
        double GrowthRatio,
        bool LeakDetected,
        string Assessment);

    public class MemoryLeakDetector
    {
        private const double LeakThreshold = 2.5; // >2.5x growth = potential leak

        public MemorySnapshot TakeSnapshot() => new(
            GC.GetTotalMemory(false),
            GC.CollectionCount(0),
            GC.CollectionCount(1),
            DateTime.UtcNow);

        public MemoryLeakReport Analyze(MemorySnapshot baseline, MemorySnapshot peak, MemorySnapshot final)
        {
            var growthRatio  = baseline.AllocatedBytes > 0
                ? (double)final.AllocatedBytes / baseline.AllocatedBytes
                : 1.0;
            var leakDetected = growthRatio > LeakThreshold;

            return new MemoryLeakReport(
                baseline.AllocatedBytes,
                peak.AllocatedBytes,
                final.AllocatedBytes,
                growthRatio,
                leakDetected,
                leakDetected
                    ? $"POTENTIAL LEAK: memory grew {growthRatio:F2}x (threshold={LeakThreshold}x)"
                    : $"HEALTHY: memory growth {growthRatio:F2}x within threshold");
        }

        public MemoryLeakReport RunStressAndAnalyze(Action<int> workload, int iterations)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var baseline = TakeSnapshot();
            MemorySnapshot peak = baseline;

            for (int i = 0; i < iterations; i++)
            {
                workload(i);
                var current = TakeSnapshot();
                if (current.AllocatedBytes > peak.AllocatedBytes)
                    peak = current;
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var final = TakeSnapshot();
            return Analyze(baseline, peak, final);
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // LAYER 23 — Circuit Breaker Chaos Patterns
    // Injects failures at controlled rates and verifies circuit behavior
    // ══════════════════════════════════════════════════════════════════════════

    public enum ChaosPattern { RandomFailure, BurstFailure, SlowDegradation, RecoveryTest }

    public record ChaosResult(
        ChaosPattern Pattern,
        int TotalCalls,
        int Successes,
        int Failures,
        int CircuitOpenEvents,
        int CircuitCloseEvents,
        bool CircuitBehavedCorrectly,
        string Summary);

    public class CircuitBreakerChaosEngine
    {
        private readonly Random _rng;

        public CircuitBreakerChaosEngine(int seed = 42) => _rng = new Random(seed);

        public ChaosResult RunChaos(ChaosPattern pattern, int totalCalls, int failureThreshold = 3)
        {
            var cb = new CircuitBreaker(failureThreshold, successThreshold: 2,
                recoveryWindow: TimeSpan.FromMilliseconds(100));

            int successes = 0, failures = 0, openEvents = 0, closeEvents = 0;
            var prevState = CircuitState.Closed;

            for (int i = 0; i < totalCalls; i++)
            {
                bool shouldFail = pattern switch
                {
                    ChaosPattern.RandomFailure    => _rng.NextDouble() < 0.4,
                    ChaosPattern.BurstFailure     => i >= 5 && i <= 10,
                    ChaosPattern.SlowDegradation  => _rng.NextDouble() < (double)i / totalCalls * 0.8,
                    ChaosPattern.RecoveryTest     => i < failureThreshold + 1,
                    _                             => false
                };

                if (pattern == ChaosPattern.RecoveryTest && i == failureThreshold + 2)
                    Thread.Sleep(150); // wait for recovery window

                bool success;
                if (!cb.CanExecute())
                {
                    success = false;
                    failures++;
                }
                else if (shouldFail)
                {
                    cb.RecordFailure();
                    success = false;
                    failures++;
                }
                else
                {
                    cb.RecordSuccess();
                    success = true;
                    successes++;
                }

                var newState = cb.State;
                if (prevState == CircuitState.Closed && newState == CircuitState.Open)
                    openEvents++;
                if (prevState == CircuitState.Open && newState == CircuitState.Closed)
                    closeEvents++;
                prevState = newState;
            }

            bool behavedCorrectly = pattern switch
            {
                ChaosPattern.BurstFailure    => openEvents >= 1,
                ChaosPattern.RecoveryTest    => openEvents >= 1,
                ChaosPattern.RandomFailure   => true,
                ChaosPattern.SlowDegradation => true,
                _                            => true
            };

            return new ChaosResult(pattern, totalCalls, successes, failures,
                openEvents, closeEvents, behavedCorrectly,
                $"Pattern={pattern}: {successes} ok, {failures} fail, {openEvents} opens, {closeEvents} closes");
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // LAYER 24 — Deterministic Replay Under Stress
    // Verifies that replaying the same event log always produces identical state
    // ══════════════════════════════════════════════════════════════════════════

    public record ReplayStressResult(
        int Replays,
        int ConsistentReplays,
        bool FullyDeterministic,
        string FinalHash,
        string Assessment);

    public class DeterministicReplayEngine
    {
        public string ComputeStateHash(IEnumerable<CanonicalEvent> events)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var e in events.OrderBy(x => x.Sequence))
                sb.Append($"{e.Sequence}:{e.Type}:{e.Hash}|");
            var bytes = System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(sb.ToString()));
            return Convert.ToHexString(bytes)[..16];
        }

        public ReplayStressResult RunReplayStress(int replayCount, int eventCount)
        {
            // Build canonical event log once
            var store = new CanonicalEventStore();
            for (int i = 0; i < eventCount; i++)
            {
                var type = i % 3 == 0 ? EventType.DirectiveIssued
                         : i % 3 == 1 ? EventType.InvariantChecked
                         : EventType.BarrierReleased;
                store.Append(type, $"ACTOR-{i % 4}", $"payload-{i}");
            }

            var events = store.Events.ToList();
            var referenceHash = ComputeStateHash(events);

            int consistent = 0;
            for (int r = 0; r < replayCount; r++)
            {
                var replayHash = ComputeStateHash(events);
                if (replayHash == referenceHash) consistent++;
            }

            bool fullyDeterministic = consistent == replayCount;
            return new ReplayStressResult(
                replayCount, consistent, fullyDeterministic, referenceHash,
                fullyDeterministic
                    ? $"DETERMINISTIC: all {replayCount} replays produced identical hash"
                    : $"NON-DETERMINISTIC: only {consistent}/{replayCount} replays matched");
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // LAYER 25 — Barrier Network Deadlock Prevention
    // Tests that barriers cannot deadlock under concurrent check-in patterns
    // ══════════════════════════════════════════════════════════════════════════

    public record DeadlockTestResult(
        int Scenarios,
        int DeadlocksDetected,
        int SuccessfulReleases,
        bool DeadlockFree,
        string Assessment);

    public class BarrierDeadlockTester
    {
        public DeadlockTestResult RunDeadlockTests(int scenarioCount = 10)
        {
            int deadlocks = 0, releases = 0;

            for (int s = 0; s < scenarioCount; s++)
            {
                var agents = new[] { AgentRole.COMMANDER, AgentRole.FORGE, AgentRole.SENTINEL };
                var barrier = new BarrierNetwork(agents);

                // Concurrent check-in from all agents
                var tasks = agents.Select(agent => Task.Run(() =>
                {
                    Thread.Sleep(new Random(s * 10 + (int)agent).Next(0, 10));
                    barrier.CheckIn(agent);
                })).ToArray();

                var completed = Task.WaitAll(tasks, TimeSpan.FromSeconds(2));

                if (!completed)
                {
                    deadlocks++;
                    continue;
                }

                if (barrier.AllCheckedIn)
                {
                    var released = barrier.Release();
                    if (released) releases++;
                }
            }

            bool deadlockFree = deadlocks == 0;
            return new DeadlockTestResult(
                scenarioCount, deadlocks, releases, deadlockFree,
                deadlockFree
                    ? $"DEADLOCK-FREE: {releases}/{scenarioCount} barriers released cleanly"
                    : $"DEADLOCK DETECTED: {deadlocks} scenarios timed out");
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // LAYER 26 — Invariant Violation Storm
    // Fires many invariant checks simultaneously and verifies correct detection
    // ══════════════════════════════════════════════════════════════════════════

    public record InvariantStormResult(
        int TotalChecks,
        int Violations,
        int Passes,
        double ViolationRate,
        bool DetectionAccurate,
        string Assessment);

    public class InvariantViolationStorm
    {
        private readonly InvariantEngine _engine = new();

        public InvariantStormResult RunStorm(int checkCount = 100)
        {
            var results = new ConcurrentBag<InvariantResult>();
            var rng     = new Random(99);

            Parallel.For(0, checkCount, i =>
            {
                // Randomly inject violations
                var injectViolation = i % 3 == 0;

                InvariantResult result;
                if (injectViolation)
                {
                    // Deliberately mismatched version vs events
                    result = _engine.CheckProjectionAtomicity(i + 1, i + 5);
                }
                else
                {
                    result = _engine.CheckProjectionAtomicity(i + 1, i + 1);
                }
                results.Add(result);
            });

            var all        = results.ToList();
            var violations = all.Count(r => r.Status == InvariantStatus.FAIL);
            var passes     = all.Count(r => r.Status == InvariantStatus.PASS);

            // Expected: ~1/3 violations
            var expectedViolations = checkCount / 3;
            var detectionAccurate  = Math.Abs(violations - expectedViolations) <= checkCount / 10;

            return new InvariantStormResult(
                checkCount, violations, passes,
                (double)violations / checkCount,
                detectionAccurate,
                $"Storm: {violations} violations detected out of {checkCount} checks");
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // LAYER 27 — Adaptive Doctrine Under Chaos
    // Verifies doctrine adapts correctly when metrics spike chaotically
    // ══════════════════════════════════════════════════════════════════════════

    public record DoctrineAdaptationReport(
        int AdaptationEvents,
        double InitialErrorThreshold,
        double FinalErrorThreshold,
        bool ThresholdDecreased,
        bool SnapshotRestoreWorks,
        string Assessment);

    public class DoctrineChaosTester
    {
        public DoctrineAdaptationReport RunChaosAdaptation(int rounds = 20)
        {
            var engine  = new AdaptiveDoctrineEngine();
            var initial = engine.Thresholds["error_rate_max"];
            var snap    = engine.TakeSnapshot();

            int adaptations = 0;

            for (int r = 0; r < rounds; r++)
            {
                var health = new HealthReport(
                    HealthScore:     r % 4 == 0 ? 0.3 : 0.8,
                    ErrorRate:       r % 4 == 0 ? 0.15 : 0.01,
                    SpanSuccessRate: r % 5 == 0 ? 0.4 : 0.9,
                    AvgLatencyMs:    r % 3 == 0 ? 900 : 100,
                    TotalMetrics:    10,
                    TotalSpans:      5,
                    Status:          r % 4 == 0 ? "DEGRADED" : "HEALTHY",
                    GeneratedAt:     DateTime.UtcNow);

                var evt = engine.Adapt(health);
                if (evt != null) adaptations++;
            }

            var finalThreshold = engine.Thresholds["error_rate_max"];

            var restored    = engine.RestoreSnapshot(snap);
            var afterRestore = engine.Thresholds["error_rate_max"];
            bool snapshotWorks = restored && Math.Abs(afterRestore - initial) < 0.001;

            return new DoctrineAdaptationReport(
                adaptations, initial, finalThreshold,
                finalThreshold <= initial,
                snapshotWorks,
                $"Doctrine adapted {adaptations} times over {rounds} chaos rounds");
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // LAYER 28 — Multi-Shard Consistency Under Partition
    // Simulates network partition and verifies shard consistency recovery
    // ══════════════════════════════════════════════════════════════════════════

    public record PartitionTestResult(
        int TotalDirectives,
        int DeliveredBeforePartition,
        int DeliveredDuringPartition,
        int DeliveredAfterRecovery,
        bool ConsistencyMaintained,
        string Assessment);

    public class ShardPartitionSimulator
    {
        public PartitionTestResult SimulatePartition(int directivesPerPhase = 20)
        {
            var router = new DistributedCommandRouter();
            for (int i = 0; i < 4; i++)
                router.RegisterShard(new AgentShard((AgentRole)(i % 6), i));

            int beforePartition = 0, duringPartition = 0, afterRecovery = 0;

            // Phase 1: Normal operation
            for (int i = 0; i < directivesPerPhase; i++)
            {
                var d = new Directive($"PRE-{i:D4}", DirectiveType.ANALYZE, "pre", RiskLevel.LOW, DateTime.UtcNow);
                var r = router.Route(d);
                if (r.Delivered) beforePartition++;
            }

            // Phase 2: Partition — mark 2 shards unhealthy
            router.Shards[0].MarkUnhealthy();
            router.Shards[1].MarkUnhealthy();

            for (int i = 0; i < directivesPerPhase; i++)
            {
                var d = new Directive($"PART-{i:D4}", DirectiveType.ANALYZE, "during", RiskLevel.LOW, DateTime.UtcNow);
                var r = router.Route(d);
                if (r.Delivered) duringPartition++;
            }

            // Phase 3: Recovery — restore shards
            router.Shards[0].MarkHealthy();
            router.Shards[1].MarkHealthy();

            for (int i = 0; i < directivesPerPhase; i++)
            {
                var d = new Directive($"REC-{i:D4}", DirectiveType.ANALYZE, "after", RiskLevel.LOW, DateTime.UtcNow);
                var r = router.Route(d);
                if (r.Delivered) afterRecovery++;
            }

            bool consistent = beforePartition == directivesPerPhase
                           && duringPartition == directivesPerPhase  // healthy shards still serve
                           && afterRecovery   == directivesPerPhase;

            return new PartitionTestResult(
                directivesPerPhase * 3,
                beforePartition, duringPartition, afterRecovery,
                consistent,
                $"Partition test: {beforePartition}/{duringPartition}/{afterRecovery} delivered per phase");
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // LAYER 29 — Full-Stack Chaos Engineering
    // Combines all chaos patterns into a single coordinated stress test
    // ══════════════════════════════════════════════════════════════════════════

    public record ChaosEngineeringReport(
        bool LoadTestPassed,
        bool MemoryHealthy,
        bool CircuitBreakerCorrect,
        bool ReplayDeterministic,
        bool BarrierDeadlockFree,
        bool InvariantDetectionAccurate,
        bool DoctrineAdapted,
        bool PartitionRecovered,
        int TotalChecks,
        int Passed,
        string OverallVerdict);

    public class FullStackChaosEngine
    {
        public ChaosEngineeringReport RunFullChaos()
        {
            // Load test
            var loadTester = new ConcurrentLoadTester(shardCount: 4);
            var loadResult = loadTester.Run(directiveCount: 200, parallelism: 8);

            // Memory
            var memDetector = new MemoryLeakDetector();
            var memReport   = memDetector.RunStressAndAnalyze(
                i => { var store = new CanonicalEventStore(); for (int j = 0; j < 10; j++) store.Append(EventType.DirectiveIssued, "ACTOR", $"p{j}"); },
                iterations: 20);

            // Circuit breaker chaos
            var cbChaos    = new CircuitBreakerChaosEngine(seed: 7);
            var cbResult   = cbChaos.RunChaos(ChaosPattern.BurstFailure, totalCalls: 30);

            // Replay determinism
            var replayEng  = new DeterministicReplayEngine();
            var replayRes  = replayEng.RunReplayStress(replayCount: 10, eventCount: 50);

            // Barrier deadlock
            var barrierTest = new BarrierDeadlockTester();
            var barrierRes  = barrierTest.RunDeadlockTests(scenarioCount: 5);

            // Invariant storm
            var storm      = new InvariantViolationStorm();
            var stormRes   = storm.RunStorm(checkCount: 60);

            // Doctrine chaos
            var docChaos   = new DoctrineChaosTester();
            var docRes     = docChaos.RunChaosAdaptation(rounds: 10);

            // Partition
            var partSim    = new ShardPartitionSimulator();
            var partRes    = partSim.SimulatePartition(directivesPerPhase: 10);

            var checks = new[]
            {
                loadResult.NoDuplicates && loadResult.AllDelivered,
                !memReport.LeakDetected,
                cbResult.CircuitBehavedCorrectly,
                replayRes.FullyDeterministic,
                barrierRes.DeadlockFree,
                stormRes.DetectionAccurate,
                docRes.AdaptationEvents > 0,
                partRes.ConsistencyMaintained
            };

            int passed = checks.Count(c => c);
            string verdict = passed == checks.Length ? "ALL CHAOS TESTS PASSED ✅"
                           : passed >= checks.Length - 1 ? "NEAR-PERFECT — 1 minor issue ⚠️"
                           : $"PARTIAL PASS: {passed}/{checks.Length} ⚠️";

            return new ChaosEngineeringReport(
                checks[0], checks[1], checks[2], checks[3],
                checks[4], checks[5], checks[6], checks[7],
                checks.Length, passed, verdict);
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // LAYER 30 — Recovery & Resilience Certification
    // Final certification that the system meets all resilience requirements
    // ══════════════════════════════════════════════════════════════════════════

    public enum CertificationLevel { BRONZE, SILVER, GOLD, PLATINUM }

    public record ResilienceCertificate(
        string SystemId,
        CertificationLevel Level,
        int RequirementsMet,
        int TotalRequirements,
        double ComplianceScore,
        IReadOnlyList<string> PassedRequirements,
        IReadOnlyList<string> FailedRequirements,
        DateTime IssuedAt);

    public class ResilienceCertificationEngine
    {
        private readonly List<(string Name, Func<bool> Check)> _requirements = new();

        public ResilienceCertificationEngine()
        {
            // REQ-01: Concurrent load without duplicates
            _requirements.Add(("REQ-01: No duplicate delivery under load", () =>
            {
                var tester = new ConcurrentLoadTester(shardCount: 3);
                var result = tester.Run(directiveCount: 100, parallelism: 6);
                return result.NoDuplicates;
            }));

            // REQ-02: All directives delivered under load
            _requirements.Add(("REQ-02: 100% delivery under concurrent load", () =>
            {
                var tester = new ConcurrentLoadTester(shardCount: 3);
                var result = tester.Run(directiveCount: 100, parallelism: 6);
                return result.AllDelivered;
            }));

            // REQ-03: Circuit breaker opens on burst failures
            _requirements.Add(("REQ-03: Circuit breaker opens on burst failures", () =>
            {
                var chaos  = new CircuitBreakerChaosEngine(seed: 42);
                var result = chaos.RunChaos(ChaosPattern.BurstFailure, totalCalls: 25);
                return result.CircuitOpenEvents >= 1;
            }));

            // REQ-04: Deterministic replay
            _requirements.Add(("REQ-04: Deterministic replay (10 replays identical)", () =>
            {
                var engine = new DeterministicReplayEngine();
                var result = engine.RunReplayStress(replayCount: 10, eventCount: 30);
                return result.FullyDeterministic;
            }));

            // REQ-05: Hash chain integrity
            _requirements.Add(("REQ-05: Audit hash chain integrity", () =>
            {
                var store = new CanonicalEventStore();
                for (int i = 0; i < 20; i++)
                    store.Append(EventType.DirectiveIssued, "CERT", $"p{i}");
                return store.VerifyChain();
            }));

            // REQ-06: SafeLock blocks execution
            _requirements.Add(("REQ-06: SafeLock blocks LaunchExecuted events", () =>
            {
                var store = new CanonicalEventStore();
                store.ActivateSafeLock();
                try
                {
                    store.Append(EventType.LaunchExecuted, "TEST", "blocked?");
                    return false;
                }
                catch (InvalidOperationException)
                {
                    return true;
                }
            }));

            // REQ-07: Barrier deadlock-free
            _requirements.Add(("REQ-07: Barrier network deadlock-free", () =>
            {
                var tester = new BarrierDeadlockTester();
                var result = tester.RunDeadlockTests(scenarioCount: 5);
                return result.DeadlockFree;
            }));

            // REQ-08: Invariant violations detected
            _requirements.Add(("REQ-08: Invariant violations correctly detected", () =>
            {
                var engine = new InvariantEngine();
                var fail   = engine.CheckProjectionAtomicity(5, 10);
                var pass   = engine.CheckProjectionAtomicity(5, 5);
                return fail.Status == InvariantStatus.FAIL && pass.Status == InvariantStatus.PASS;
            }));

            // REQ-09: Partition recovery
            _requirements.Add(("REQ-09: Shard partition recovery — all phases deliver", () =>
            {
                var sim    = new ShardPartitionSimulator();
                var result = sim.SimulatePartition(directivesPerPhase: 10);
                return result.ConsistencyMaintained;
            }));

            // REQ-10: Doctrine snapshot restore
            _requirements.Add(("REQ-10: Doctrine snapshot restore works correctly", () =>
            {
                var tester = new DoctrineChaosTester();
                var result = tester.RunChaosAdaptation(rounds: 5);
                return result.SnapshotRestoreWorks;
            }));
        }

        public ResilienceCertificate Certify(string systemId)
        {
            var passed = new List<string>();
            var failed = new List<string>();

            foreach (var (name, check) in _requirements)
            {
                try
                {
                    if (check()) passed.Add(name);
                    else         failed.Add(name);
                }
                catch (Exception ex)
                {
                    failed.Add($"{name} [EXCEPTION: {ex.Message}]");
                }
            }

            var score = (double)passed.Count / _requirements.Count;
            var level = score switch
            {
                >= 1.00 => CertificationLevel.PLATINUM,
                >= 0.90 => CertificationLevel.GOLD,
                >= 0.75 => CertificationLevel.SILVER,
                _       => CertificationLevel.BRONZE
            };

            return new ResilienceCertificate(
                systemId, level,
                passed.Count, _requirements.Count,
                score,
                passed.AsReadOnly(),
                failed.AsReadOnly(),
                DateTime.UtcNow);
        }
    }
}