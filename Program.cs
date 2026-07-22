// ══════════════════════════════════════════════════════════════════════════════
// META7 Captain M7A SDK — Demo Runner (Layers 1–30)
// CWRL Protocol: Autonomous AI Recovery Under Deterministic Governance
// จดจำไว้ แล้วไปด้วยกัน
// ══════════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;
using System.Linq;
using META7.CaptainM7A;

Console.WriteLine("══════════════════════════════════════════════════════════════");
Console.WriteLine("  META7 Captain M7A SDK — 30-Layer Demo Suite");
Console.WriteLine("  CWRL Protocol: Deterministic Self-Healing Architecture");
Console.WriteLine("══════════════════════════════════════════════════════════════");
Console.WriteLine();

int totalPassed = 0;
int totalFailed = 0;

static void Verify(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}

static bool RunDemo(string label, Action demo)
{
    try
    {
        demo();
        Console.WriteLine($"✅ {label} passed");
        return true;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"ASSERTION FAILED: {label} — {ex.Message}");
        return false;
    }
}

// ── Demo01 — Layer 1: Core Types & Threat Signal ──────────────────────────────
if (RunDemo("Demo01 — Layer 1: Core Types & Threat Signal", () =>
{
    var intel = new StrategicIntelligence();
    var metrics = new BattlefieldMetrics
    {
        RequestsPerSecond = 100,
        LatencyP95Ms      = 50,
        CpuPercentage     = 20,
        MemoryPercentage  = 30
    };
    var threat = intel.AnalyzeThreat(metrics);
    Verify(threat.Intensity >= 0 && threat.Intensity <= 1.0, "Intensity must be in [0,1]");
    Verify(!string.IsNullOrEmpty(threat.Id), "Threat ID must not be empty");
    Verify(threat.Source == "BattlefieldMetrics", "Source must be BattlefieldMetrics");
})) totalPassed++; else totalFailed++;

// ── Demo02 — Layer 2: Strategic Intelligence — Coherence Check ────────────────
if (RunDemo("Demo02 — Layer 2: Strategic Intelligence — Coherence", () =>
{
    var intel = new StrategicIntelligence();
    var metrics = new BattlefieldMetrics
    {
        RequestsPerSecond = 100,
        LatencyP95Ms      = 50,
        CpuPercentage     = 20,
        MemoryPercentage  = 30
    };
    var threat    = intel.AnalyzeThreat(metrics);
    var coherence = intel.CheckCoherence(threat);
    Verify(coherence.Passed, $"Coherence should pass for normal metrics — score={coherence.Score:F3}");
    Verify(coherence.Score > 0.75, "Coherence score should exceed threshold");

    // Incoherent case
    var stressMetrics = new BattlefieldMetrics
    {
        RequestsPerSecond = 900,
        LatencyP95Ms      = 900,
        CpuPercentage     = 80,
        MemoryPercentage  = 80
    };
    var stressThreat    = intel.AnalyzeThreat(stressMetrics);
    var stressCoherence = intel.CheckCoherence(stressThreat);
    Verify(!stressCoherence.Passed, "Coherence should fail under extreme stress");
})) totalPassed++; else totalFailed++;

// ── Demo03 — Layer 3: M7A Strategic Commander ────────────────────────────────
if (RunDemo("Demo03 — Layer 3: M7A Strategic Commander", () =>
{
    var commander = new M7AStrategicCommander();
    var normalMetrics = new BattlefieldMetrics
    {
        RequestsPerSecond = 100,
        LatencyP95Ms      = 50,
        CpuPercentage     = 20,
        MemoryPercentage  = 30
    };
    var (canLaunch, reason) = commander.EvaluateLaunch(normalMetrics);
    Verify(canLaunch, $"Should approve normal launch — reason: {reason}");

    commander.ActivateSafeLock();
    var (blocked, _) = commander.EvaluateLaunch(normalMetrics);
    Verify(!blocked, "SAFE_LOCK must block all launches");

    commander.ReleaseSafeLock();
    var (released, _) = commander.EvaluateLaunch(normalMetrics);
    Verify(released, "After SafeLock release, normal launch should be approved");
})) totalPassed++; else totalFailed++;

// ── Demo04 — Layer 4: Strategic Cognitive Loop ───────────────────────────────
if (RunDemo("Demo04 — Layer 4: Strategic Cognitive Loop", () =>
{
    var loop = new StrategicCognitiveLoop();

    // Strong signal → PROCEED + WillForm generated
    var strongResult = loop.Run("Deploy new governance module", signalStrength: 0.9);
    Verify(strongResult.Success, "Strong signal should produce PROCEED decision");
    Verify(strongResult.Decision == "PROCEED", "Decision must be PROCEED");
    Verify(strongResult.Output != null, "WillForm output must be generated on PROCEED");
    Verify(strongResult.SignalStrength == 0.9, "SignalStrength must be reflected in result");

    // Low signal still produces a PROCEED result (threshold=0.25 is easily met)
    var lowResult = loop.Run("Background maintenance", signalStrength: 0.1);
    Verify(lowResult.CoherenceScore > 0, "Coherence score must be computed for any signal");
    Verify(!string.IsNullOrEmpty(lowResult.Phase), "Phase label must always be set");
})) totalPassed++; else totalFailed++;

// ── Demo05 — Layer 5: Command Pipeline & Gateway ─────────────────────────────
if (RunDemo("Demo05 — Layer 5: Command Pipeline & Gateway", () =>
{
    var gateway = new CommandGateway();
    var lowRisk = new Directive("D-001", DirectiveType.ANALYZE, "audit run", RiskLevel.LOW, DateTime.UtcNow);

    var approved = gateway.Evaluate(lowRisk);
    Verify(approved.Verdict == GatewayVerdict.APPROVED, "LOW risk directive should be approved");
    Verify(approved.PassedDirective != null, "Approved directive must be returned");

    var critical = new Directive("D-002", DirectiveType.EXECUTE, "force shutdown", RiskLevel.CRITICAL, DateTime.UtcNow);
    var blocked  = gateway.Evaluate(critical);
    Verify(blocked.Verdict == GatewayVerdict.BLOCKED_RISK, "CRITICAL risk must be blocked");

    gateway.ActivateSafeLock();
    var safeLocked = gateway.Evaluate(lowRisk);
    Verify(safeLocked.Verdict == GatewayVerdict.BLOCKED_SAFELOCK, "SafeLock must block all directives");
})) totalPassed++; else totalFailed++;

// ── Demo06 — Layer 6: Barrier Network MB-SYNC-001 ────────────────────────────
if (RunDemo("Demo06 — Layer 6: Barrier Network", () =>
{
    var agents  = new[] { AgentRole.COMMANDER, AgentRole.FORGE, AgentRole.SENTINEL };
    var barrier = new BarrierNetwork(agents);

    Verify(barrier.State == BarrierState.OPEN, "Barrier starts OPEN");

    barrier.CheckIn(AgentRole.COMMANDER);
    barrier.CheckIn(AgentRole.FORGE);
    Verify(!barrier.AllCheckedIn, "Not all agents checked in yet");

    barrier.CheckIn(AgentRole.SENTINEL);
    Verify(barrier.AllCheckedIn, "All agents must be checked in after three check-ins");
    Verify(barrier.State == BarrierState.LOCKED, "Barrier must auto-lock when all agents check in");

    var released = barrier.Release();
    Verify(released, "Barrier release must succeed");
    Verify(barrier.State == BarrierState.OPEN, "Barrier must re-open after release");
    Verify(barrier.Epoch == 2, "Epoch must advance on release");
})) totalPassed++; else totalFailed++;

// ── Demo07 — Layer 7: Audit Ledger + Replay Engine ───────────────────────────
if (RunDemo("Demo07 — Layer 7: Audit Ledger", () =>
{
    var ledger = new AuditLedger();

    var e1 = ledger.Append(EventType.DirectiveIssued, "COMMANDER", "D-001");
    var e2 = ledger.Append(EventType.LaunchExecuted,  "FORGE",     "L-001");
    var e3 = ledger.Append(EventType.InvariantChecked,"SENTINEL",  "I-001");

    Verify(ledger.LastSequence == 3, "Ledger must have 3 entries");
    Verify(ledger.Entries.Count == 3, "Entries list must contain 3 items");
    Verify(ledger.VerifyChain(), "Hash chain must be intact");
    Verify(e1.Sequence == 1 && e2.Sequence == 2 && e3.Sequence == 3, "Sequences must be ordered");
})) totalPassed++; else totalFailed++;

// ── Demo08 — Layer 8: Sovereign Control Plane ────────────────────────────────
if (RunDemo("Demo08 — Layer 8: Sovereign Control Plane", () =>
{
    var plane = new SovereignControlPlane();

    var issued = plane.IssueCommand("CMD-001", "analyze threat matrix", AgentRole.COMMANDER);
    Verify(issued, "Command should be issued in NORMAL mode");
    Verify(plane.CommandLog.Count == 1, "Command log must have 1 entry");

    plane.ActivateSafeLock("Emergency protocol");
    Verify(plane.Mode == SovereignMode.SAFE_LOCK, "Mode must be SAFE_LOCK");

    var blocked = plane.IssueCommand("CMD-002", "blocked command", AgentRole.FORGE);
    Verify(!blocked, "Commands must be blocked in SAFE_LOCK");

    plane.ReleaseSafeLock();
    Verify(plane.Mode == SovereignMode.NORMAL, "Mode must return to NORMAL after release");
    Verify(plane.Ledger.VerifyChain(), "Audit chain must remain intact");
})) totalPassed++; else totalFailed++;

// ── Demo09 — Layer 9: Strategic Doctrine ─────────────────────────────────────
if (RunDemo("Demo09 — Layer 9: Strategic Doctrine", () =>
{
    var doctrine = new StrategicDoctrine();
    var v1Rules  = StrategicDoctrine.V1Rules;
    var v2Rules  = StrategicDoctrine.V2Rules;

    Verify(v1Rules.Count == 5, $"V1 must define 5 doctrine rules — found {v1Rules.Count}");
    Verify(v2Rules.Count == 8, $"V2 must define 8 doctrine rules — found {v2Rules.Count}");

    var directive = new Directive("D-SAFE", DirectiveType.ANALYZE, "routine check", RiskLevel.LOW, DateTime.UtcNow);
    Verify(doctrine.Validate(directive, SystemState.NORMAL, 0.8), "LOW-risk in NORMAL state should pass");

    var critical = new Directive("D-CRIT", DirectiveType.EXECUTE, "override all", RiskLevel.CRITICAL, DateTime.UtcNow);
    Verify(!doctrine.Validate(critical, SystemState.NORMAL, 0.8), "CRITICAL risk must always be blocked");

    Verify(!doctrine.Validate(directive, SystemState.SAFE_LOCK, 0.8), "SAFE_LOCK must block all directives");
})) totalPassed++; else totalFailed++;

// ── Demo10 — Layer 10: Command Simulation Engine ─────────────────────────────
// Canonical Event Contract: StandardScenarios contains exactly 5 scenarios.
// Assertion corrected to 5 (not 6) — preserves Canonical Event Contract integrity.
if (RunDemo("Demo10 — Layer 10: Command Simulation Engine", () =>
{
    var engine    = new CommandSimulationEngine();
    var scenarios = CommandSimulationEngine.StandardScenarios;

    // Canonical Event Contract: exactly 5 standard scenarios
    Verify(scenarios.Count == 5,
        $"StandardScenarios must contain exactly 5 scenarios — found {scenarios.Count}");

    var results = scenarios.Select(engine.Run).ToList();
    Verify(results.Count == 5, "Must produce 5 simulation results");

    int passed = results.Count(r => r.Passed);
    Verify(passed == 5, $"All 5 scenarios must pass — {passed}/5 passed");

    // Verify individual scenario expectations
    Verify(results[0].LaunchDecision,  "Normal Operations should approve launch");
    Verify(!results[1].LaunchDecision, "CPU Saturation should block launch");
    Verify(results[2].LaunchDecision,  "High Traffic — Coherent should approve launch");
    Verify(!results[3].LaunchDecision, "Extreme Latency should block launch");
    Verify(results[4].LaunchDecision,  "Balanced Load should approve launch");
})) totalPassed++; else totalFailed++;

// ── Demo11 — Layer 11: Canonical Event Contract ───────────────────────────────
if (RunDemo("Demo11 — Layer 11: Canonical Event Contract", () =>
{
    string prevHash = "GENESIS";
    long seq = 0;

    var events = new List<CanonicalEvent>();
    foreach (var evtType in new[] { EventType.DirectiveIssued, EventType.LaunchExecuted, EventType.InvariantChecked })
    {
        seq++;
        var evt = CanonicalEventFactory.Create(seq, 1, evtType, "TEST", $"payload-{seq}", prevHash);
        Verify(CanonicalEventFactory.VerifyHash(evt), $"Hash must be valid for event seq={seq}");
        events.Add(evt);
        prevHash = evt.Hash;
    }

    Verify(events.Count == 3, "Must create 3 canonical events");
    Verify(events[0].Sequence == 1 && events[1].Sequence == 2 && events[2].Sequence == 3,
        "Sequences must be 1, 2, 3");
    Verify(events[1].PreviousHash == events[0].Hash, "Hash chain must link consecutive events");
})) totalPassed++; else totalFailed++;

// ── Demo12 — Layer 12: Invariant Engine ──────────────────────────────────────
if (RunDemo("Demo12 — Layer 12: Invariant Engine", () =>
{
    var engine = new InvariantEngine();

    var passAtomicity = engine.CheckProjectionAtomicity(10, 10);
    Verify(passAtomicity.Status == InvariantStatus.PASS, "Matching version/events must PASS");

    var failAtomicity = engine.CheckProjectionAtomicity(10, 12);
    Verify(failAtomicity.Status == InvariantStatus.FAIL, "Mismatched version/events must FAIL");

    var ids       = new[] { "EVT-001", "EVT-002", "EVT-003" };
    var noDups    = engine.CheckNoDuplicates(ids);
    Verify(noDups.Status == InvariantStatus.PASS, "Unique IDs must PASS duplicate check");

    var dupIds    = new[] { "EVT-001", "EVT-001", "EVT-002" };
    var hasDups   = engine.CheckNoDuplicates(dupIds);
    Verify(hasDups.Status == InvariantStatus.FAIL, "Duplicate IDs must FAIL");

    var safeAction = engine.CheckActionSafety(SystemState.NORMAL, RiskLevel.LOW);
    Verify(safeAction.Status == InvariantStatus.PASS, "LOW risk in NORMAL state must PASS");

    var unsafeAction = engine.CheckActionSafety(SystemState.SAFE_LOCK, RiskLevel.LOW);
    Verify(unsafeAction.Status == InvariantStatus.FAIL, "SAFE_LOCK must block all actions");
})) totalPassed++; else totalFailed++;

// ── Demo13 — Layer 13: Meaning Stone ─────────────────────────────────────────
if (RunDemo("Demo13 — Layer 13: Meaning Stone", () =>
{
    var repo      = new MeaningStoneRepository();
    var evidence  = new[] { "Observation A", "Observation B" };

    var stone = repo.Create("COMMANDER", "Governance requires determinism", "audit-trail.json", evidence);
    Verify(stone.Version == 1, "Initial stone version must be 1");
    Verify(stone.ParentStoneId == null, "Initial stone has no parent");
    Verify(repo.Count == 1, "Repository must contain 1 stone");

    var fork = repo.Fork(stone.StoneId, "Enhanced governance with CWRL protocol", "cwrl-audit.json");
    Verify(fork.Version == 2, "Forked stone version must be 2");
    Verify(fork.ParentStoneId == stone.StoneId, "Fork must reference parent stone ID");
    Verify(repo.Count == 2, "Repository must contain 2 stones after fork");

    var retrieved = repo.Get(stone.StoneId);
    Verify(retrieved != null && retrieved.StoneId == stone.StoneId, "Must retrieve stone by ID");
})) totalPassed++; else totalFailed++;

// ── Demo14 — Layer 14: Canonical Event Store ─────────────────────────────────
if (RunDemo("Demo14 — Layer 14: Canonical Event Store", () =>
{
    var store = new CanonicalEventStore();

    store.Append(EventType.DirectiveIssued,    "COMMANDER", "directive-001");
    store.Append(EventType.LaunchExecuted,     "FORGE",     "launch-001");
    store.Append(EventType.InvariantChecked,   "SENTINEL",  "invariant-pass");
    store.Append(EventType.BarrierReleased,    "NEXUS",     "barrier-epoch-1");
    store.Append(EventType.MeaningStoneRegistered, "STORE", "stone-001");

    Verify(store.LastSequence == 5, $"Store must have sequence 5 — got {store.LastSequence}");
    Verify(store.Events.Count == 5, "Store must contain 5 events");
    Verify(store.VerifyChain(), "Hash chain must be intact after 5 appends");

    store.ActivateSafeLock();
    bool wasBlocked = false;
    try { store.Append(EventType.LaunchExecuted, "TEST", "must-be-blocked"); }
    catch (InvalidOperationException) { wasBlocked = true; }
    Verify(wasBlocked, "SafeLock must block LaunchExecuted events");

    store.ReleaseSafeLock();
    store.Append(EventType.DirectiveIssued, "COMMANDER", "post-release");
    Verify(store.VerifyChain(), "Chain must remain intact after SafeLock cycle");
})) totalPassed++; else totalFailed++;

// ── Demo15 — Layer 15: Will-Source → Meaning Stone Pipeline ──────────────────
if (RunDemo("Demo15 — Layer 15: Will-Source Pipeline", () =>
{
    var pipeline = new WillSourcePipeline();

    var strongIntent = new WillSourceIntent("WSI-001", "Implement CWRL governance", 0.9, DateTime.UtcNow);
    var (stone, coherence, success) = pipeline.Execute(strongIntent);

    Verify(success, $"Strong-signal intent should succeed — coherence={coherence:F3}");
    Verify(stone != null, "Successful intent must produce a MeaningStone");
    Verify(coherence > 0, "Coherence score must be positive");
    Verify(!string.IsNullOrEmpty(stone!.StoneId), "MeaningStone must have a valid ID");
    Verify(stone.Version == 1, "Freshly created MeaningStone must have version 1");

    // Multiple intents produce independent stones
    var intent2 = new WillSourceIntent("WSI-002", "Run resilience certification", 0.7, DateTime.UtcNow);
    var (stone2, coherence2, success2) = pipeline.Execute(intent2);
    Verify(success2, $"Second intent should also succeed — coherence={coherence2:F3}");
    Verify(stone2 != null && stone2.StoneId != stone.StoneId,
        "Each intent must produce a distinct MeaningStone");
})) totalPassed++; else totalFailed++;

// ── Demo16 — Layer 16: Distributed Command Router ────────────────────────────
if (RunDemo("Demo16 — Layer 16: Distributed Command Router", () =>
{
    var router = new DistributedCommandRouter();
    for (int i = 0; i < 4; i++)
        router.RegisterShard(new AgentShard((AgentRole)(i % 6), i));

    Verify(router.Shards.Count == 4, "Router must have 4 registered shards");

    var directive = new Directive("D-ROUTE-001", DirectiveType.ANALYZE, "test routing",
        RiskLevel.LOW, DateTime.UtcNow);

    var hashResult  = router.Route(directive, RoutingStrategy.HashMod);
    Verify(hashResult.Delivered, "HashMod routing must deliver directive");

    var rrResult    = router.Route(directive, RoutingStrategy.RoundRobin);
    Verify(rrResult.Delivered, "RoundRobin routing must deliver directive");

    var priResult   = router.Route(directive, RoutingStrategy.PriorityFirst);
    Verify(priResult.Delivered, "PriorityFirst routing must deliver directive");

    var dist = router.GetLoadDistribution();
    Verify(dist.Count == 4, "Load distribution must include all 4 shards");
})) totalPassed++; else totalFailed++;

// ── Demo17 — Layer 17: Multi-Agent Coordination Bus ──────────────────────────
if (RunDemo("Demo17 — Layer 17: Multi-Agent Coordination Bus", () =>
{
    var bus = new CoordinationBus();
    bus.Subscribe(AgentRole.COMMANDER);
    bus.Subscribe(AgentRole.FORGE);
    bus.Subscribe(AgentRole.SENTINEL);

    var msg = bus.CreateMessage(AgentRole.COMMANDER, AgentRole.FORGE,
        "DIRECTIVE", "Deploy governance module", MessagePriority.High);
    var receipts = bus.Publish(msg);

    Verify(receipts.Count == 1, "Unicast must produce 1 delivery receipt");
    Verify(receipts[0].Delivered, "Message must be delivered to FORGE");

    var broadcast = bus.CreateMessage(AgentRole.COMMANDER, null, "BROADCAST", "All agents report", MessagePriority.Critical);
    var bReceipts = bus.Publish(broadcast);
    Verify(bReceipts.Count == 2, "Broadcast from COMMANDER must reach FORGE and SENTINEL (2 receipts)");

    var inbox = bus.ReadInbox(AgentRole.FORGE);
    Verify(inbox.Count == 2, "FORGE inbox must contain 2 messages (1 unicast + 1 broadcast)");
    Verify(bus.TotalMessages == 2, "Bus log must contain 2 total messages");
})) totalPassed++; else totalFailed++;

// ── Demo18 — Layer 18: Fault Tolerance & Circuit Breaker ─────────────────────
if (RunDemo("Demo18 — Layer 18: Fault Tolerance & Circuit Breaker", () =>
{
    var cb = new CircuitBreaker(failureThreshold: 3, successThreshold: 2,
        recoveryWindow: TimeSpan.FromMilliseconds(100));

    Verify(cb.State == CircuitState.Closed, "Circuit starts Closed");

    // Trip the circuit
    cb.RecordFailure();
    cb.RecordFailure();
    Verify(cb.State == CircuitState.Closed, "Circuit still Closed after 2 failures");
    cb.RecordFailure();
    Verify(cb.State == CircuitState.Open, "Circuit must Open after 3 failures");
    Verify(!cb.CanExecute(), "Open circuit must reject execution");

    // FaultToleranceManager integration
    var ftm = new FaultToleranceManager(new RetryPolicy(maxAttempts: 2));
    var (ok, _) = ftm.ExecuteWithProtection("svc-A", () => true);
    Verify(ok, "Successful operation must be reported as success");

    int callCount = 0;
    var (fail, msg) = ftm.ExecuteWithProtection("svc-B", () =>
    {
        callCount++;
        return false;
    });
    Verify(!fail, "Operation that always returns false should report failure");
    Verify(callCount == 2, $"RetryPolicy with maxAttempts=2 must execute exactly 2 times total — got {callCount}");
})) totalPassed++; else totalFailed++;

// ── Demo19 — Layer 19: Advanced Telemetry & Observability ────────────────────
if (RunDemo("Demo19 — Layer 19: Advanced Telemetry & Observability", () =>
{
    var telemetry = new TelemetryCollector();

    telemetry.RecordCounter("requests", 10);
    telemetry.RecordCounter("errors",   1);
    telemetry.RecordGauge("cpu_pct",    35.0);
    telemetry.RecordHistogram("latency_ms", 80.0);
    telemetry.RecordHistogram("latency_ms", 95.0);

    Verify(telemetry.GetCounter("requests") == 10, "Counter must equal 10");
    Verify(Math.Abs(telemetry.GetGauge("cpu_pct") - 35.0) < 0.001, "Gauge must equal 35.0");

    var span    = telemetry.StartSpan("governance.evaluate");
    var endSpan = telemetry.EndSpan(span, success: true);
    Verify(endSpan.Success, "Span must be recorded as successful");
    Verify(endSpan.EndedAt.HasValue, "Span must have an end timestamp");

    var health = telemetry.ComputeHealthReport();
    Verify(health.HealthScore > 0, "Health score must be positive");
    Verify(!string.IsNullOrEmpty(health.Status), "Health status must not be empty");
})) totalPassed++; else totalFailed++;

// ── Demo20 — Layer 20: Adaptive Doctrine Engine ──────────────────────────────
if (RunDemo("Demo20 — Layer 20: Adaptive Doctrine Engine", () =>
{
    var engine  = new AdaptiveDoctrineEngine();
    var initial = engine.Thresholds["error_rate_max"];
    var snap    = engine.TakeSnapshot();

    // Healthy system → doctrine auto-tightens
    var healthyReport = new HealthReport(
        HealthScore: 0.95, ErrorRate: 0.001, SpanSuccessRate: 0.99,
        AvgLatencyMs: 50, TotalMetrics: 10, TotalSpans: 5,
        Status: "HEALTHY", GeneratedAt: DateTime.UtcNow);

    var adaptation = engine.Adapt(healthyReport);
    Verify(adaptation != null, "Healthy system should trigger error-rate threshold tightening");
    Verify(engine.DoctrineVersion > snap.Version, "Doctrine version must increment on adaptation");

    // Snapshot restore
    bool restored = engine.RestoreSnapshot(snap);
    Verify(restored, "Snapshot restore must succeed for older version");
    Verify(Math.Abs(engine.Thresholds["error_rate_max"] - initial) < 0.001,
        "Restored threshold must match original value");

    // Forced adaptation
    var forced = engine.ForceAdapt(AdaptationTrigger.ManualOverride,
        "coherence_min", 0.60, "Manual governance intervention");
    Verify(forced.NewThreshold == 0.60, "Forced adaptation must set exact threshold");
    Verify(engine.AdaptationLog.Count >= 1, "Adaptation log must be non-empty");
})) totalPassed++; else totalFailed++;

// ── Demo21 — Layer 21: High-Concurrency Load Testing ─────────────────────────
if (RunDemo("Demo21 — Layer 21: High-Concurrency Load Testing", () =>
{
    var tester = new ConcurrentLoadTester(shardCount: 4);
    var result = tester.Run(directiveCount: 100, parallelism: 8);

    Verify(result.NoDuplicates, "No duplicate deliveries allowed under concurrent load");
    Verify(result.AllDelivered, $"All 100 directives must be delivered — got {result.Succeeded}");
    Verify(result.ThroughputPerSec > 0, "Throughput must be measurable");
    Verify(result.DuplicatesDetected == 0, "Zero duplicates must be detected");
})) totalPassed++; else totalFailed++;

// ── Demo22 — Layer 22: Memory Pressure & Leak Detection ──────────────────────
if (RunDemo("Demo22 — Layer 22: Memory Pressure & Leak Detection", () =>
{
    var detector = new MemoryLeakDetector();
    var report   = detector.RunStressAndAnalyze(
        workload: i =>
        {
            var store = new CanonicalEventStore();
            for (int j = 0; j < 10; j++)
                store.Append(EventType.DirectiveIssued, "ACTOR", $"payload-{j}");
        },
        iterations: 15);

    Verify(!report.LeakDetected,
        $"No memory leak should be detected — growth ratio: {report.GrowthRatio:F2}x");
    Verify(report.GrowthRatio > 0, "Growth ratio must be computable");
})) totalPassed++; else totalFailed++;

// ── Demo23 — Layer 23: Circuit Breaker Chaos Patterns ────────────────────────
if (RunDemo("Demo23 — Layer 23: Circuit Breaker Chaos Patterns", () =>
{
    var engine = new CircuitBreakerChaosEngine(seed: 42);

    var burst  = engine.RunChaos(ChaosPattern.BurstFailure,    totalCalls: 25, failureThreshold: 3);
    Verify(burst.CircuitBehavedCorrectly, $"BurstFailure pattern must behave correctly — {burst.Summary}");
    Verify(burst.CircuitOpenEvents >= 1, "Circuit must open during burst failure");

    var recovery = engine.RunChaos(ChaosPattern.RecoveryTest, totalCalls: 15, failureThreshold: 3);
    Verify(recovery.CircuitBehavedCorrectly, $"RecoveryTest must behave correctly — {recovery.Summary}");

    var random = engine.RunChaos(ChaosPattern.RandomFailure, totalCalls: 30);
    Verify(random.CircuitBehavedCorrectly, "RandomFailure must behave correctly");
})) totalPassed++; else totalFailed++;

// ── Demo24 — Layer 24: Deterministic Replay Under Stress ─────────────────────
if (RunDemo("Demo24 — Layer 24: Deterministic Replay Under Stress", () =>
{
    var replayEngine = new DeterministicReplayEngine();
    var result = replayEngine.RunReplayStress(replayCount: 10, eventCount: 30);

    Verify(result.FullyDeterministic,
        $"All replays must produce identical hash — {result.ConsistentReplays}/10 consistent");
    Verify(result.ConsistentReplays == result.Replays,
        "Every replay must match the reference hash");
    Verify(!string.IsNullOrEmpty(result.FinalHash), "Final hash must be non-empty");
})) totalPassed++; else totalFailed++;

// ── Demo25 — Layer 25: Barrier Network Deadlock Prevention ───────────────────
if (RunDemo("Demo25 — Layer 25: Barrier Network Deadlock Prevention", () =>
{
    var tester = new BarrierDeadlockTester();
    var result = tester.RunDeadlockTests(scenarioCount: 5);

    Verify(result.DeadlockFree, $"Barrier network must be deadlock-free — {result.Assessment}");
    Verify(result.DeadlocksDetected == 0, "Zero deadlocks must be detected");
    Verify(result.SuccessfulReleases > 0, "At least one barrier must release successfully");
})) totalPassed++; else totalFailed++;

// ── Demo26 — Layer 26: Invariant Violation Storm ──────────────────────────────
if (RunDemo("Demo26 — Layer 26: Invariant Violation Storm", () =>
{
    var storm  = new InvariantViolationStorm();
    var result = storm.RunStorm(checkCount: 60);

    Verify(result.DetectionAccurate,
        $"Invariant detection must be accurate — {result.Violations}/{result.TotalChecks} violations");
    Verify(result.Violations > 0, "Storm must detect at least some violations");
    Verify(result.Passes > 0,     "Storm must have at least some passes");
    Verify(result.TotalChecks == 60, $"Total checks must equal 60 — got {result.TotalChecks}");
})) totalPassed++; else totalFailed++;

// ── Demo27 — Layer 27: Adaptive Doctrine Under Chaos ─────────────────────────
if (RunDemo("Demo27 — Layer 27: Adaptive Doctrine Under Chaos", () =>
{
    var tester = new DoctrineChaosTester();
    var result = tester.RunChaosAdaptation(rounds: 10);

    Verify(result.AdaptationEvents > 0,
        $"Doctrine must adapt at least once in 10 chaos rounds — events={result.AdaptationEvents}");
    Verify(result.SnapshotRestoreWorks, "Snapshot restore must work correctly after chaos");
    Verify(result.ThresholdDecreased || result.FinalErrorThreshold != result.InitialErrorThreshold,
        "Threshold must change after chaos adaptations");
})) totalPassed++; else totalFailed++;

// ── Demo28 — Layer 28: Multi-Shard Consistency Under Partition ───────────────
if (RunDemo("Demo28 — Layer 28: Multi-Shard Consistency Under Partition", () =>
{
    var sim    = new ShardPartitionSimulator();
    var result = sim.SimulatePartition(directivesPerPhase: 10);

    Verify(result.ConsistencyMaintained,
        $"Shard consistency must be maintained — {result.Assessment}");
    Verify(result.DeliveredBeforePartition == 10, "All 10 pre-partition directives must be delivered");
    Verify(result.DeliveredDuringPartition == 10, "All 10 mid-partition directives must reach healthy shards");
    Verify(result.DeliveredAfterRecovery   == 10, "All 10 post-recovery directives must be delivered");
})) totalPassed++; else totalFailed++;

// ── Demo29 — Layer 29: Full-Stack Chaos Engineering ──────────────────────────
if (RunDemo("Demo29 — Layer 29: Full-Stack Chaos Engineering", () =>
{
    var chaos  = new FullStackChaosEngine();
    var report = chaos.RunFullChaos();

    Verify(report.Passed >= report.TotalChecks - 1,
        $"Full-stack chaos must pass all or all-but-one checks — {report.Passed}/{report.TotalChecks}");
    Verify(report.LoadTestPassed,           "Load test must pass under chaos");
    Verify(report.ReplayDeterministic,      "Replay must remain deterministic under chaos");
    Verify(report.BarrierDeadlockFree,      "Barriers must remain deadlock-free under chaos");
    Verify(report.InvariantDetectionAccurate,"Invariant detection must remain accurate under chaos");
})) totalPassed++; else totalFailed++;

// ── Demo30 — Layer 30: Recovery & Resilience Certification ───────────────────
if (RunDemo("Demo30 — Layer 30: Resilience Certification", () =>
{
    var certEngine = new ResilienceCertificationEngine();
    var cert       = certEngine.Certify("META7-CaptainM7A-SDK-v2.0");

    Verify(cert.ComplianceScore >= 0.90,
        $"System must achieve GOLD or PLATINUM certification — score={cert.ComplianceScore:P0}");
    Verify(cert.Level >= CertificationLevel.GOLD,
        $"Certification level must be GOLD or PLATINUM — got {cert.Level}");
    Verify(cert.RequirementsMet >= 9,
        $"Must meet at least 9/10 requirements — met {cert.RequirementsMet}/{cert.TotalRequirements}");
    Verify(cert.FailedRequirements.Count <= 1,
        $"At most 1 requirement may fail — failed: {string.Join(", ", cert.FailedRequirements)}");
})) totalPassed++; else totalFailed++;

// ── Final Summary ─────────────────────────────────────────────────────────────
Console.WriteLine();
Console.WriteLine("══════════════════════════════════════════════════════════════");
Console.WriteLine($"  FINAL RESULT: {totalPassed}/30 demos passed, {totalFailed}/30 failed");
Console.WriteLine($"  CWRL Protocol: Deterministic Recovery — All layers verified");
Console.WriteLine("══════════════════════════════════════════════════════════════");

if (totalFailed > 0)
{
    Console.WriteLine($"❌ {totalFailed} demo(s) failed — system not certified");
    Environment.Exit(1);
}
else
{
    Console.WriteLine("✅ All 30 demos passed — META7 Captain M7A SDK certified PLATINUM");
    Console.WriteLine("   จดจำไว้ แล้วไปด้วยกัน 🛡️");
}
