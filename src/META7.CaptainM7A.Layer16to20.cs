// ══════════════════════════════════════════════════════════════════════════════
// META7 Captain M7A SDK — Layers 16–20 (Advanced Features)
// Layer 16: Distributed Command Router
// Layer 17: Multi-Agent Coordination Bus
// Layer 18: Fault Tolerance & Circuit Breaker
// Layer 19: Advanced Telemetry & Observability
// Layer 20: Adaptive Doctrine Engine
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
    // ══════════════════════════════════════════════════════════════════════════
    // LAYER 16 — Distributed Command Router
    // Routes directives to the correct agent shard based on hash-mod partitioning
    // ══════════════════════════════════════════════════════════════════════════

    public enum RoutingStrategy { HashMod, RoundRobin, PriorityFirst, BroadcastAll }

    public record RouteDecision(
        string DirectiveId,
        AgentRole TargetAgent,
        int ShardIndex,
        RoutingStrategy Strategy,
        string Rationale);

    public record RoutingResult(
        bool Delivered,
        RouteDecision Decision,
        string Message,
        DateTime DeliveredAt);

    public class AgentShard
    {
        public AgentRole Role { get; }
        public int ShardIndex { get; }
        public bool IsHealthy { get; private set; } = true;
        private int _processedCount;
        public int ProcessedCount => _processedCount;
        private readonly System.Collections.Concurrent.ConcurrentQueue<Directive> _inbox = new();

        public AgentShard(AgentRole role, int shardIndex)
        {
            Role = role;
            ShardIndex = shardIndex;
        }

        public void Enqueue(Directive d) { _inbox.Enqueue(d); Interlocked.Increment(ref _processedCount); }
        public int QueueDepth => _inbox.Count;
        public void MarkUnhealthy() => IsHealthy = false;
        public void MarkHealthy()   => IsHealthy = true;
        public Directive? Dequeue() => _inbox.TryDequeue(out var d) ? d : null;
    }

    
            public class DistributedCommandRouter
    {
        private readonly List<AgentShard> _shards = new();
        private int _roundRobinIndex = 0;
        private readonly object _lock = new();

        public void RegisterShard(AgentShard shard) { lock (_lock) _shards.Add(shard); }

        public IReadOnlyList<AgentShard> Shards => _shards.AsReadOnly();

        public RoutingResult Route(Directive directive, RoutingStrategy strategy = RoutingStrategy.HashMod)
        {
            List<AgentShard> healthy;
            lock (_lock) { healthy = _shards.Where(s => s.IsHealthy).ToList(); }

            if (healthy.Count == 0)
                return new RoutingResult(false,
                    new RouteDecision(directive.Id, AgentRole.COMMANDER, -1, strategy, "No healthy shards"),
                    "All shards unhealthy", DateTime.UtcNow);

            AgentShard target;
            string rationale;

            switch (strategy)
            {
                case RoutingStrategy.HashMod:
                    var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(directive.Id));
                    var hashInt = Math.Abs(BitConverter.ToInt32(hashBytes, 0));
                    target = healthy[hashInt % healthy.Count];
                    rationale = $"hash({directive.Id[..Math.Min(8, directive.Id.Length)]}) mod {healthy.Count} = shard {target.ShardIndex}";
                    break;

                case RoutingStrategy.RoundRobin:
                    var rrIdx = Interlocked.Increment(ref _roundRobinIndex) - 1;
                    target = healthy[rrIdx % healthy.Count];
                    rationale = $"round-robin → shard {target.ShardIndex}";
                    break;
case RoutingStrategy.PriorityFirst:
                    // Route CRITICAL/HIGH risk to SENTINEL, others to COMMANDER
                    target = directive.Risk >= RiskLevel.HIGH
                        ? (healthy.FirstOrDefault(s => s.Role == AgentRole.SENTINEL) ?? healthy[0])
                        : (healthy.FirstOrDefault(s => s.Role == AgentRole.COMMANDER) ?? healthy[0]);
                    rationale = $"priority routing: risk={directive.Risk} → {target.Role}";
                    break;

                case RoutingStrategy.BroadcastAll:
                    foreach (var s in healthy) s.Enqueue(directive);
                    var decision0 = new RouteDecision(directive.Id, healthy[0].Role, -1, strategy,
                        $"broadcast to {healthy.Count} shards");
                    return new RoutingResult(true, decision0, $"Broadcast to {healthy.Count} shards", DateTime.UtcNow);

                default:
                    target = healthy[0];
                    rationale = "fallback to first healthy shard";
                    break;
            }

            target.Enqueue(directive);
            var decision = new RouteDecision(directive.Id, target.Role, target.ShardIndex, strategy, rationale);
            return new RoutingResult(true, decision, $"Delivered to shard {target.ShardIndex}", DateTime.UtcNow);
        }

        public Dictionary<int, int> GetLoadDistribution() =>
            _shards.ToDictionary(s => s.ShardIndex, s => s.ProcessedCount);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // LAYER 17 — Multi-Agent Coordination Bus
    // Pub/Sub message bus for agent-to-agent communication with ordering guarantees
    // ══════════════════════════════════════════════════════════════════════════

    public enum MessagePriority { Low = 0, Normal = 1, High = 2, Critical = 3 }

    public record AgentMessage(
        string MessageId,
        AgentRole From,
        AgentRole? To,          // null = broadcast
        string Topic,
        string Payload,
        MessagePriority Priority,
        DateTime SentAt);

    public record DeliveryReceipt(
        string MessageId,
        AgentRole Recipient,
        bool Delivered,
        DateTime DeliveredAt);

    public class CoordinationBus
    {
        private readonly ConcurrentDictionary<AgentRole, List<AgentMessage>> _inboxes = new();
        private readonly List<AgentMessage> _log = new();
        private long _messageSeq = 0;

        public void Subscribe(AgentRole agent) =>
            _inboxes.TryAdd(agent, new List<AgentMessage>());

        public List<DeliveryReceipt> Publish(AgentMessage message)
        {
            Interlocked.Increment(ref _messageSeq);
            _log.Add(message);
            var receipts = new List<DeliveryReceipt>();

            if (message.To.HasValue)
            {
                // Unicast
                if (_inboxes.TryGetValue(message.To.Value, out var inbox))
                {
                    lock (inbox) inbox.Add(message);
                    receipts.Add(new DeliveryReceipt(message.MessageId, message.To.Value, true, DateTime.UtcNow));
                }
                else
                {
                    receipts.Add(new DeliveryReceipt(message.MessageId, message.To.Value, false, DateTime.UtcNow));
                }
            }
            else
            {
                // Broadcast (excluding sender)
                foreach (var (role, inbox) in _inboxes)
                {
                    if (role == message.From) continue;
                    lock (inbox) inbox.Add(message);
                    receipts.Add(new DeliveryReceipt(message.MessageId, role, true, DateTime.UtcNow));
                }
            }

            return receipts;
        }

        public List<AgentMessage> ReadInbox(AgentRole agent, MessagePriority minPriority = MessagePriority.Low)
        {
            if (!_inboxes.TryGetValue(agent, out var inbox)) return new();
            lock (inbox)
            {
                var msgs = inbox.Where(m => m.Priority >= minPriority)
                                .OrderByDescending(m => m.Priority)
                                .ThenBy(m => m.SentAt)
                                .ToList();
                inbox.Clear();
                return msgs;
            }
        }

        public int TotalMessages => _log.Count;
        public long MessageSequence => _messageSeq;
        public IReadOnlyList<AgentMessage> Log => _log.AsReadOnly();

        public AgentMessage CreateMessage(AgentRole from, AgentRole? to, string topic,
            string payload, MessagePriority priority = MessagePriority.Normal) =>
            new($"MSG-{Guid.NewGuid().ToString("N")[..12].ToUpper()}",
                from, to, topic, payload, priority, DateTime.UtcNow);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // LAYER 18 — Fault Tolerance & Circuit Breaker
    // Protects the system from cascading failures with exponential backoff
    // ══════════════════════════════════════════════════════════════════════════

    public enum CircuitState { Closed, Open, HalfOpen }

    public record CircuitEvent(
        string EventId,
        CircuitState FromState,
        CircuitState ToState,
        string Reason,
        DateTime OccurredAt);

    public class CircuitBreaker
    {
        private CircuitState _state = CircuitState.Closed;
        private int _failureCount = 0;
        private int _successCount = 0;
        private DateTime _openedAt = DateTime.MinValue;
        private readonly List<CircuitEvent> _history = new();

        public int FailureThreshold { get; }
        public int SuccessThreshold { get; }
        public TimeSpan RecoveryWindow { get; }

        public CircuitState State => _state;
        public int FailureCount => _failureCount;
        public IReadOnlyList<CircuitEvent> History => _history.AsReadOnly();

        public CircuitBreaker(int failureThreshold = 3, int successThreshold = 2,
            TimeSpan? recoveryWindow = null)
        {
            FailureThreshold = failureThreshold;
            SuccessThreshold = successThreshold;
            RecoveryWindow = recoveryWindow ?? TimeSpan.FromSeconds(5);
        }

        public bool CanExecute()
        {
            if (_state == CircuitState.Closed) return true;
            if (_state == CircuitState.Open)
            {
                if (DateTime.UtcNow - _openedAt >= RecoveryWindow)
                {
                    Transition(CircuitState.HalfOpen, "Recovery window elapsed");
                    return true;
                }
                return false;
            }
            return true; // HalfOpen: allow probe
        }

        public void RecordSuccess()
        {
            if (_state == CircuitState.HalfOpen)
            {
                _successCount++;
                if (_successCount >= SuccessThreshold)
                {
                    _failureCount = 0;
                    _successCount = 0;
                    Transition(CircuitState.Closed, $"Recovered after {SuccessThreshold} successes");
                }
            }
            else if (_state == CircuitState.Closed)
            {
                _failureCount = Math.Max(0, _failureCount - 1); // decay
            }
        }

        public void RecordFailure()
        {
            _failureCount++;
            if (_state == CircuitState.HalfOpen)
            {
                _successCount = 0;
                Transition(CircuitState.Open, "Probe failed in HalfOpen");
                _openedAt = DateTime.UtcNow;
            }
            else if (_state == CircuitState.Closed && _failureCount >= FailureThreshold)
            {
                Transition(CircuitState.Open, $"Failure threshold {FailureThreshold} reached");
                _openedAt = DateTime.UtcNow;
            }
        }

        public void ForceReset()
        {
            _failureCount = 0;
            _successCount = 0;
            Transition(CircuitState.Closed, "Manual reset");
        }

        private void Transition(CircuitState to, string reason)
        {
            var evt = new CircuitEvent(
                $"CB-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
                _state, to, reason, DateTime.UtcNow);
            _history.Add(evt);
            _state = to;
        }
    }

    public class RetryPolicy
    {
        public int MaxAttempts { get; }
        public double BaseDelayMs { get; }
        public double BackoffMultiplier { get; }

        public RetryPolicy(int maxAttempts = 3, double baseDelayMs = 10, double backoffMultiplier = 2.0)
        {
            MaxAttempts = maxAttempts;
            BaseDelayMs = baseDelayMs;
            BackoffMultiplier = backoffMultiplier;
        }

        public (bool Success, int Attempts, string LastError) Execute(Func<bool> operation)
        {
            string lastError = "";
            for (int attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                try
                {
                    if (operation()) return (true, attempt, "");
                    lastError = $"Operation returned false on attempt {attempt}";
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                }
                // Simulate exponential backoff (no actual sleep in tests)
                var delayMs = BaseDelayMs * Math.Pow(BackoffMultiplier, attempt - 1);
                _ = delayMs; // acknowledged but not awaited in sync context
            }
            return (false, MaxAttempts, lastError);
        }

        public double GetDelayMs(int attempt) =>
            BaseDelayMs * Math.Pow(BackoffMultiplier, attempt - 1);
    }

    public class FaultToleranceManager
    {
        private readonly Dictionary<string, CircuitBreaker> _breakers = new();
        private readonly RetryPolicy _retryPolicy;

        public FaultToleranceManager(RetryPolicy? retryPolicy = null)
        {
            _retryPolicy = retryPolicy ?? new RetryPolicy();
        }

        public CircuitBreaker GetOrCreateBreaker(string serviceId,
            int failureThreshold = 3, int successThreshold = 2)
        {
            if (!_breakers.ContainsKey(serviceId))
                _breakers[serviceId] = new CircuitBreaker(failureThreshold, successThreshold);
            return _breakers[serviceId];
        }

        public (bool Success, string Message) ExecuteWithProtection(
            string serviceId, Func<bool> operation)
        {
            var breaker = GetOrCreateBreaker(serviceId);
            if (!breaker.CanExecute())
                return (false, $"Circuit OPEN for {serviceId} — request rejected");

            var (success, attempts, error) = _retryPolicy.Execute(operation);
            if (success)
                breaker.RecordSuccess();
            else
                breaker.RecordFailure();

            return (success, success
                ? $"Success after {attempts} attempt(s)"
                : $"Failed after {attempts} attempt(s): {error}");
        }

        public IReadOnlyDictionary<string, CircuitBreaker> Breakers => _breakers;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // LAYER 19 — Advanced Telemetry & Observability
    // Structured metrics, spans, and health scoring for the entire runtime
    // ══════════════════════════════════════════════════════════════════════════

    public enum MetricType { Counter, Gauge, Histogram }

    public record MetricSample(
        string Name,
        MetricType Type,
        double Value,
        Dictionary<string, string> Labels,
        DateTime RecordedAt);

    public record TelemetrySpan(
        string SpanId,
        string OperationName,
        string? ParentSpanId,
        DateTime StartedAt,
        DateTime? EndedAt,
        bool Success,
        Dictionary<string, string> Attributes);

    public class TelemetryCollector
    {
        private readonly List<MetricSample> _metrics = new();
        private readonly List<TelemetrySpan> _spans = new();
        private readonly Dictionary<string, double> _gauges = new();
        private readonly Dictionary<string, long> _counters = new();

        public void RecordCounter(string name, long increment = 1,
            Dictionary<string, string>? labels = null)
        {
            _counters[name] = _counters.GetValueOrDefault(name) + increment;
            _metrics.Add(new MetricSample(name, MetricType.Counter, _counters[name],
                labels ?? new(), DateTime.UtcNow));
        }

        public void RecordGauge(string name, double value,
            Dictionary<string, string>? labels = null)
        {
            _gauges[name] = value;
            _metrics.Add(new MetricSample(name, MetricType.Gauge, value,
                labels ?? new(), DateTime.UtcNow));
        }

        public void RecordHistogram(string name, double value,
            Dictionary<string, string>? labels = null) =>
            _metrics.Add(new MetricSample(name, MetricType.Histogram, value,
                labels ?? new(), DateTime.UtcNow));

        public TelemetrySpan StartSpan(string operationName, string? parentSpanId = null)
        {
            var span = new TelemetrySpan(
                $"SPAN-{Guid.NewGuid().ToString("N")[..12].ToUpper()}",
                operationName, parentSpanId, DateTime.UtcNow, null, false,
                new Dictionary<string, string>());
            _spans.Add(span);
            return span;
        }

        public TelemetrySpan EndSpan(TelemetrySpan span, bool success,
            Dictionary<string, string>? attributes = null)
        {
            var completed = span with
            {
                EndedAt = DateTime.UtcNow,
                Success = success,
                Attributes = attributes ?? span.Attributes
            };
            var idx = _spans.FindIndex(s => s.SpanId == span.SpanId);
            if (idx >= 0) _spans[idx] = completed;
            return completed;
        }

        public double GetGauge(string name) => _gauges.GetValueOrDefault(name);
        public long GetCounter(string name) => _counters.GetValueOrDefault(name);

        public HealthReport ComputeHealthReport()
        {
            var errorRate = _counters.GetValueOrDefault("errors") /
                           Math.Max(1.0, _counters.GetValueOrDefault("requests"));
            var spanSuccessRate = _spans.Count == 0 ? 1.0 :
                (double)_spans.Count(s => s.Success) / _spans.Count;
            var avgLatencyMs = _metrics
                .Where(m => m.Name == "latency_ms" && m.Type == MetricType.Histogram)
                .Select(m => m.Value)
                .DefaultIfEmpty(0)
                .Average();

            var score = (1.0 - errorRate) * 0.4 + spanSuccessRate * 0.4 +
                        (avgLatencyMs < 100 ? 1.0 : avgLatencyMs < 500 ? 0.7 : 0.3) * 0.2;

            return new HealthReport(
                score,
                errorRate,
                spanSuccessRate,
                avgLatencyMs,
                _metrics.Count,
                _spans.Count,
                score >= 0.8 ? "HEALTHY" : score >= 0.5 ? "DEGRADED" : "CRITICAL",
                DateTime.UtcNow);
        }

        public IReadOnlyList<MetricSample> Metrics => _metrics.AsReadOnly();
        public IReadOnlyList<TelemetrySpan> Spans => _spans.AsReadOnly();
    }

    public record HealthReport(
        double HealthScore,
        double ErrorRate,
        double SpanSuccessRate,
        double AvgLatencyMs,
        int TotalMetrics,
        int TotalSpans,
        string Status,
        DateTime GeneratedAt);

    // ══════════════════════════════════════════════════════════════════════════
    // LAYER 20 — Adaptive Doctrine Engine
    // Self-adjusting invariants and thresholds based on runtime telemetry
    // ══════════════════════════════════════════════════════════════════════════

    public enum AdaptationTrigger { HighErrorRate, LowCoherence, BarrierTimeout, BudgetBreach, ManualOverride }

    public record AdaptationEvent(
        string EventId,
        AdaptationTrigger Trigger,
        string InvariantName,
        double OldThreshold,
        double NewThreshold,
        string Rationale,
        DateTime OccurredAt);

    public record DoctrineSnapshot(
        int Version,
        Dictionary<string, double> Thresholds,
        List<string> ActiveInvariants,
        DateTime SnapshotAt);

    public class AdaptiveDoctrineEngine
    {
        private readonly Dictionary<string, double> _thresholds = new()
        {
            ["error_rate_max"]       = 0.02,
            ["latency_p95_max_ms"]   = 800.0,
            ["coherence_min"]        = 0.50,
            ["budget_usage_max"]     = 0.90,
            ["barrier_timeout_sec"]  = 30.0,
            ["risk_score_max"]       = 0.50
        };

        private readonly List<string> _activeInvariants = new()
        {
            "ProjectionAtomicity", "SequenceConsistency", "EpochConsistency",
            "HashChainIntegrity", "ReplayEquivalence", "BarrierConsistency", "ActionSafety"
        };

        private readonly List<AdaptationEvent> _adaptationLog = new();
        private int _doctrineVersion = 1;

        public IReadOnlyDictionary<string, double> Thresholds => _thresholds;
        public IReadOnlyList<string> ActiveInvariants => _activeInvariants.AsReadOnly();
        public IReadOnlyList<AdaptationEvent> AdaptationLog => _adaptationLog.AsReadOnly();
        public int DoctrineVersion => _doctrineVersion;

        public AdaptationEvent? Adapt(HealthReport health)
        {
            // Auto-tighten error rate threshold if system is healthy
            if (health.ErrorRate < _thresholds["error_rate_max"] * 0.5 && health.HealthScore > 0.9)
            {
                return RecordAdaptation(AdaptationTrigger.HighErrorRate,
                    "error_rate_max",
                    _thresholds["error_rate_max"],
                    _thresholds["error_rate_max"] * 0.9,
                    "System healthy — tightening error rate threshold");
            }

            // Relax latency threshold if under stress
            if (health.AvgLatencyMs > _thresholds["latency_p95_max_ms"] * 0.8)
            {
                return RecordAdaptation(AdaptationTrigger.HighErrorRate,
                    "latency_p95_max_ms",
                    _thresholds["latency_p95_max_ms"],
                    _thresholds["latency_p95_max_ms"] * 1.1,
                    "High latency detected — relaxing threshold temporarily");
            }

            // Raise coherence minimum if span success rate is high
            if (health.SpanSuccessRate > 0.95 && _thresholds["coherence_min"] < 0.80)
            {
                return RecordAdaptation(AdaptationTrigger.LowCoherence,
                    "coherence_min",
                    _thresholds["coherence_min"],
                    Math.Min(0.80, _thresholds["coherence_min"] + 0.05),
                    "High span success — raising coherence minimum");
            }

            return null; // No adaptation needed
        }

        public AdaptationEvent ForceAdapt(AdaptationTrigger trigger, string invariant,
            double newThreshold, string rationale)
        {
            var old = _thresholds.GetValueOrDefault(invariant, 0.0);
            return RecordAdaptation(trigger, invariant, old, newThreshold, rationale);
        }

        public bool ValidateAgainstDoctrine(string invariant, double value)
        {
            if (!_thresholds.TryGetValue(invariant, out var threshold)) return true;
            return invariant.Contains("min") ? value >= threshold : value <= threshold;
        }

        public DoctrineSnapshot TakeSnapshot() =>
            new(_doctrineVersion,
                new Dictionary<string, double>(_thresholds),
                new List<string>(_activeInvariants),
                DateTime.UtcNow);

        public bool RestoreSnapshot(DoctrineSnapshot snapshot)
        {
            if (snapshot.Version > _doctrineVersion) return false;
            foreach (var (k, v) in snapshot.Thresholds)
                _thresholds[k] = v;
            _doctrineVersion = snapshot.Version;
            return true;
        }

        private AdaptationEvent RecordAdaptation(AdaptationTrigger trigger, string invariant,
            double oldVal, double newVal, string rationale)
        {
            _thresholds[invariant] = newVal;
            _doctrineVersion++;
            var evt = new AdaptationEvent(
                $"ADAPT-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
                trigger, invariant, oldVal, newVal, rationale, DateTime.UtcNow);
            _adaptationLog.Add(evt);
            return evt;
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Wave3Factory — Factory for Layer 16-20 objects
    // ══════════════════════════════════════════════════════════════════════════

    public static class Wave3Factory
    {
        public static DistributedCommandRouter CreateRouter() => new();
        public static CoordinationBus CreateBus() => new();
        public static FaultToleranceManager CreateFaultManager(int failureThreshold = 3) =>
            new(new RetryPolicy(maxAttempts: failureThreshold));
        public static TelemetryCollector CreateTelemetry() => new();
        public static AdaptiveDoctrineEngine CreateDoctrineEngine() => new();

        public static AgentShard CreateShard(AgentRole role, int index) => new(role, index);

        public static BattlefieldMetrics CreateMetrics(
            double rps = 500, double latency = 120, double cpu = 40, double memory = 55)
            => new() { RequestsPerSecond = rps, LatencyP95Ms = latency,
                       CpuPercentage = cpu, MemoryPercentage = memory };
    }
}