// ============================================================
// META7 Captain M7A — Workflow Engine V3
// Execution Authority for Multi-Agent Orchestration
//
// Architecture:
//   CoordinationBus  = Communication Fabric  (transport)
//   WorkflowEngine   = Execution Fabric      (authority)
//
// Planes:
//   Control Plane  — WorkflowEngine, StepScheduler, RetryPolicy
//   Data Plane     — CoordinationBus, Channel<T> delivery
//   Metadata Plane — CorrelationId, CausationId, WorkflowId, TenantId
// ============================================================

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace META7.CaptainM7A.WorkflowEngineV3
{
    // ──────────────────────────────────────────────────────────
    // METADATA PLANE — First-Class OS Metadata
    // Analogous to Linux: PID, PPID, UID, GID
    // ──────────────────────────────────────────────────────────

    public enum MessageState
    {
        Queued,       // ส่งเข้าคิวแล้ว
        Dispatched,   // ส่งถึง agent แล้ว
        Processing,   // agent กำลัง execute
        Completed,    // execute สำเร็จ
        Failed,       // execute ล้มเหลว (retry ได้)
        DeadLetter    // หมด retry / poison message
    }

    public enum WorkflowState
    {
        Created,
        Running,
        Suspended,    // รอ external event
        Compensating, // rollback กำลังทำงาน
        Completed,
        Failed,
        TimedOut
    }

    public enum StepState
    {
        Pending,
        Running,
        Completed,
        Failed,
        Skipped,
        Compensating
    }

    public enum CapabilityType
    {
        Reasoning,
        Retrieval,
        Verification,
        Synthesis,
        Execution,
        Monitoring,
        Coordination
    }

    // OS-level message metadata — ไม่ใช่ optional field
    public record MessageMetadata(
        string MessageId,
        string CorrelationId,   // trace ทั้ง workflow
        string CausationId,     // message ที่ trigger message นี้
        string WorkflowId,
        string? TenantId,
        int Priority,           // 0=Critical, 1=High, 2=Normal, 3=Low
        DateTime DeadlineUtc,
        int RetryCount = 0,
        string? StepId = null
    );

    // ──────────────────────────────────────────────────────────
    // MESSAGE ACK MODEL
    // DeliveryReceipt = "ส่งเข้าคิวสำเร็จ" (ไม่ใช่ execute สำเร็จ)
    // MessageAck      = "agent ประมวลผลสำเร็จ/ล้มเหลว"
    // ──────────────────────────────────────────────────────────

    public record MessageAck(
        string MessageId,
        string WorkflowId,
        string StepId,
        MessageState State,
        string? ErrorMessage,
        DateTime AckedAt,
        object? Result = null
    );

    // Dead Letter — สำหรับ operation team ตามหาผี
    public record DeadLetterMessage(
        MessageMetadata OriginalMetadata,
        string Payload,
        string Reason,
        DateTime FailedAt,
        int TotalAttempts,
        Exception? LastException = null
    );

    // ──────────────────────────────────────────────────────────
    // WORKFLOW DEFINITION
    // ──────────────────────────────────────────────────────────

    public record RetryPolicy(
        int MaxAttempts,
        TimeSpan BaseDelay,
        double BackoffMultiplier = 2.0,
        TimeSpan? MaxDelay = null
    )
    {
        public static RetryPolicy None => new(1, TimeSpan.Zero);
        public static RetryPolicy Default => new(3, TimeSpan.FromSeconds(1));
        public static RetryPolicy Aggressive => new(5, TimeSpan.FromMilliseconds(200), 1.5);

        public TimeSpan GetDelay(int attempt)
        {
            var delay = TimeSpan.FromMilliseconds(
                BaseDelay.TotalMilliseconds * Math.Pow(BackoffMultiplier, attempt - 1));
            return MaxDelay.HasValue && delay > MaxDelay.Value ? MaxDelay.Value : delay;
        }
    }

    public record CompensationPolicy(
        bool Enabled,
        string? CompensationStepId = null,  // step ที่จะรัน rollback
        bool PropagateToParent = false
    )
    {
        public static CompensationPolicy None => new(false);
        public static CompensationPolicy Rollback(string stepId) => new(true, stepId);
    }

    public record WorkflowStep(
        string StepId,
        string Name,
        CapabilityType CapabilityRequired,
        string? NextStepId,                 // null = terminal step
        string? CompensationStepId = null,  // step สำหรับ rollback
        TimeSpan? Timeout = null,
        RetryPolicy? RetryPolicy = null,
        Dictionary<string, object>? Parameters = null
    )
    {
        public TimeSpan EffectiveTimeout => Timeout ?? TimeSpan.FromSeconds(30);
        public RetryPolicy EffectiveRetry => RetryPolicy ?? RetryPolicy.Default;
    }

    public record WorkflowDefinition(
        string DefinitionId,
        string Name,
        string Version,
        string EntryStepId,
        IReadOnlyList<WorkflowStep> Steps,
        TimeSpan? GlobalTimeout = null,
        CompensationPolicy? CompensationPolicy = null
    )
    {
        public WorkflowStep? GetStep(string stepId) =>
            Steps.FirstOrDefault(s => s.StepId == stepId);

        public WorkflowStep? GetEntryStep() => GetStep(EntryStepId);
    }

    // ──────────────────────────────────────────────────────────
    // WORKFLOW INSTANCE — Runtime State
    // ──────────────────────────────────────────────────────────

    public class WorkflowStepInstance
    {
        public string StepId { get; }
        public string Name { get; }
        public StepState State { get; private set; } = StepState.Pending;
        public int AttemptCount { get; private set; } = 0;
        public DateTime? StartedAt { get; private set; }
        public DateTime? CompletedAt { get; private set; }
        public string? ErrorMessage { get; private set; }
        public object? Result { get; private set; }
        public List<string> AuditLog { get; } = new();

        public WorkflowStepInstance(string stepId, string name)
        {
            StepId = stepId;
            Name = name;
        }

        public void Start()
        {
            State = StepState.Running;
            StartedAt = DateTime.UtcNow;
            AttemptCount++;
            AuditLog.Add($"[{DateTime.UtcNow:O}] STARTED attempt={AttemptCount}");
        }

        public void Complete(object? result = null)
        {
            State = StepState.Completed;
            CompletedAt = DateTime.UtcNow;
            Result = result;
            AuditLog.Add($"[{DateTime.UtcNow:O}] COMPLETED");
        }

        public void Fail(string error)
        {
            State = StepState.Failed;
            ErrorMessage = error;
            AuditLog.Add($"[{DateTime.UtcNow:O}] FAILED error={error}");
        }

        public void MarkRetrying()
        {
            State = StepState.Pending;
            AuditLog.Add($"[{DateTime.UtcNow:O}] RETRYING attempt={AttemptCount + 1}");
        }

        public void Skip(string reason)
        {
            State = StepState.Skipped;
            AuditLog.Add($"[{DateTime.UtcNow:O}] SKIPPED reason={reason}");
        }
    }

    public class WorkflowInstance
    {
        public string WorkflowId { get; }
        public string CorrelationId { get; }
        public string DefinitionId { get; }
        public WorkflowState State { get; private set; } = WorkflowState.Created;
        public string? CurrentStepId { get; private set; }
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
        public DateTime? StartedAt { get; private set; }
        public DateTime? CompletedAt { get; private set; }
        public DateTime? DeadlineUtc { get; }
        public string? TenantId { get; }
        public string? ErrorMessage { get; private set; }

        private readonly Dictionary<string, WorkflowStepInstance> _steps = new();
        private readonly List<string> _auditLog = new();
        private readonly object _lock = new();

        public IReadOnlyDictionary<string, WorkflowStepInstance> Steps => _steps;
        public IReadOnlyList<string> AuditLog => _auditLog;

        public WorkflowInstance(
            string workflowId,
            string correlationId,
            string definitionId,
            DateTime? deadlineUtc = null,
            string? tenantId = null)
        {
            WorkflowId = workflowId;
            CorrelationId = correlationId;
            DefinitionId = definitionId;
            DeadlineUtc = deadlineUtc;
            TenantId = tenantId;
        }

        public bool IsTimedOut => DeadlineUtc.HasValue && DateTime.UtcNow > DeadlineUtc.Value;

        public void Start(string entryStepId)
        {
            lock (_lock)
            {
                State = WorkflowState.Running;
                CurrentStepId = entryStepId;
                StartedAt = DateTime.UtcNow;
                Audit($"WORKFLOW STARTED entry={entryStepId}");
            }
        }

        public WorkflowStepInstance GetOrCreateStep(string stepId, string name)
        {
            lock (_lock)
            {
                if (!_steps.TryGetValue(stepId, out var step))
                {
                    step = new WorkflowStepInstance(stepId, name);
                    _steps[stepId] = step;
                }
                return step;
            }
        }

        public void AdvanceTo(string nextStepId)
        {
            lock (_lock)
            {
                CurrentStepId = nextStepId;
                Audit($"ADVANCED to step={nextStepId}");
            }
        }

        public void Complete()
        {
            lock (_lock)
            {
                State = WorkflowState.Completed;
                CompletedAt = DateTime.UtcNow;
                CurrentStepId = null;
                Audit("WORKFLOW COMPLETED");
            }
        }

        public void Fail(string error)
        {
            lock (_lock)
            {
                State = WorkflowState.Failed;
                ErrorMessage = error;
                CompletedAt = DateTime.UtcNow;
                Audit($"WORKFLOW FAILED error={error}");
            }
        }

        public void TimeOut()
        {
            lock (_lock)
            {
                State = WorkflowState.TimedOut;
                CompletedAt = DateTime.UtcNow;
                Audit("WORKFLOW TIMED OUT");
            }
        }

        public void StartCompensation()
        {
            lock (_lock)
            {
                State = WorkflowState.Compensating;
                Audit("COMPENSATION STARTED");
            }
        }

        private void Audit(string msg) =>
            _auditLog.Add($"[{DateTime.UtcNow:O}] {msg}");
    }

    // ──────────────────────────────────────────────────────────
    // DEAD LETTER QUEUE
    // ──────────────────────────────────────────────────────────

    public class DeadLetterQueue
    {
        private readonly ConcurrentQueue<DeadLetterMessage> _queue = new();
        private long _totalEnqueued = 0;

        public void Enqueue(DeadLetterMessage msg)
        {
            _queue.Enqueue(msg);
            Interlocked.Increment(ref _totalEnqueued);
        }

        public IReadOnlyList<DeadLetterMessage> Drain()
        {
            var result = new List<DeadLetterMessage>();
            while (_queue.TryDequeue(out var msg))
                result.Add(msg);
            return result;
        }

        public int Count => _queue.Count;
        public long TotalEnqueued => _totalEnqueued;

        public IReadOnlyList<DeadLetterMessage> PeekAll() => _queue.ToArray();
    }

    // ──────────────────────────────────────────────────────────
    // PRIORITY MULTI-LANE QUEUE
    // 4 lanes: Critical(0) High(1) Normal(2) Low(3)
    // ──────────────────────────────────────────────────────────

    public class PriorityMessageQueue<T>
    {
        private readonly Channel<T>[] _lanes;
        private readonly int _laneCount;

        public PriorityMessageQueue(int laneCount = 4, int capacityPerLane = 1024)
        {
            _laneCount = laneCount;
            _lanes = new Channel<T>[laneCount];
            for (int i = 0; i < laneCount; i++)
            {
                _lanes[i] = Channel.CreateBounded<T>(new BoundedChannelOptions(capacityPerLane)
                {
                    FullMode = BoundedChannelFullMode.Wait,
                    SingleReader = false,
                    SingleWriter = false
                });
            }
        }

        public async ValueTask EnqueueAsync(T item, int priority, CancellationToken ct = default)
        {
            var lane = Math.Clamp(priority, 0, _laneCount - 1);
            await _lanes[lane].Writer.WriteAsync(item, ct);
        }

        // Drain highest priority lane first
        public async ValueTask<T> DequeueAsync(CancellationToken ct = default)
        {
            while (!ct.IsCancellationRequested)
            {
                for (int lane = 0; lane < _laneCount; lane++)
                {
                    if (_lanes[lane].Reader.TryRead(out var item))
                        return item;
                }
                // ไม่มีข้อความใน lane ไหนเลย — รอสั้น ๆ
                await Task.Delay(1, ct);
            }
            throw new OperationCanceledException(ct);
        }

        public bool TryDequeue(out T? item)
        {
            for (int lane = 0; lane < _laneCount; lane++)
            {
                if (_lanes[lane].Reader.TryRead(out item))
                    return true;
            }
            item = default;
            return false;
        }

        public int TotalCount => _lanes.Sum(l =>
        {
            l.Reader.TryPeek(out _);
            return (int)l.Reader.Count;
        });

        public void Complete()
        {
            foreach (var lane in _lanes)
                lane.Writer.TryComplete();
        }
    }

    // ──────────────────────────────────────────────────────────
    // WORKFLOW ENGINE V3 — Execution Authority
    // ──────────────────────────────────────────────────────────

    public interface IAgentExecutor
    {
        CapabilityType Capability { get; }
        string AgentId { get; }

        // Execute step และ return result
        // throw exception = failure
        Task<object?> ExecuteAsync(
            WorkflowStep step,
            WorkflowInstance workflow,
            MessageMetadata metadata,
            CancellationToken ct);
    }

    public class WorkflowEngineV3
    {
        private readonly ConcurrentDictionary<string, WorkflowDefinition> _definitions = new();
        private readonly ConcurrentDictionary<string, WorkflowInstance> _instances = new();
        private readonly ConcurrentDictionary<CapabilityType, List<IAgentExecutor>> _executors = new();
        private readonly DeadLetterQueue _dlq = new();
        private readonly PriorityMessageQueue<(WorkflowInstance, WorkflowStep, MessageMetadata)> _workQueue;
        private long _totalCompleted = 0;
        private long _totalFailed = 0;

        public DeadLetterQueue DeadLetterQueue => _dlq;
        public long TotalCompleted => _totalCompleted;
        public long TotalFailed => _totalFailed;
        public int ActiveInstances => _instances.Count(kv =>
            kv.Value.State == WorkflowState.Running ||
            kv.Value.State == WorkflowState.Compensating);

        public WorkflowEngineV3(int priorityLanes = 4)
        {
            _workQueue = new PriorityMessageQueue<(WorkflowInstance, WorkflowStep, MessageMetadata)>(priorityLanes);
        }

        // ── Registration ──────────────────────────────────────

        public void RegisterDefinition(WorkflowDefinition def) =>
            _definitions[def.DefinitionId] = def;

        public void RegisterExecutor(IAgentExecutor executor)
        {
            _executors.AddOrUpdate(
                executor.Capability,
                _ => new List<IAgentExecutor> { executor },
                (_, list) => { lock (list) { list.Add(executor); } return list; });
        }

        // ── Workflow Lifecycle ────────────────────────────────

        public WorkflowInstance CreateInstance(
            string definitionId,
            string? correlationId = null,
            string? tenantId = null,
            TimeSpan? timeout = null)
        {
            if (!_definitions.TryGetValue(definitionId, out var def))
                throw new InvalidOperationException($"Definition '{definitionId}' not found");

            var workflowId = $"WF-{Guid.NewGuid():N}"[..20].ToUpper();
            var corrId = correlationId ?? $"CORR-{Guid.NewGuid():N}"[..12].ToUpper();
            var deadline = timeout.HasValue ? DateTime.UtcNow + timeout.Value : (DateTime?)null;

            var instance = new WorkflowInstance(workflowId, corrId, definitionId, deadline, tenantId);
            _instances[workflowId] = instance;
            return instance;
        }

        public async Task<WorkflowInstance> ExecuteAsync(
            string workflowId,
            int priority = 2,
            CancellationToken ct = default)
        {
            if (!_instances.TryGetValue(workflowId, out var instance))
                throw new InvalidOperationException($"Instance '{workflowId}' not found");

            if (!_definitions.TryGetValue(instance.DefinitionId, out var def))
                throw new InvalidOperationException($"Definition '{instance.DefinitionId}' not found");

            var entryStep = def.GetEntryStep()
                ?? throw new InvalidOperationException("No entry step defined");

            instance.Start(entryStep.StepId);

            await RunWorkflowAsync(instance, def, entryStep, priority, ct);
            return instance;
        }

        private async Task RunWorkflowAsync(
            WorkflowInstance instance,
            WorkflowDefinition def,
            WorkflowStep currentStep,
            int priority,
            CancellationToken ct)
        {
            while (currentStep != null && !ct.IsCancellationRequested)
            {
                // Timeout check
                if (instance.IsTimedOut)
                {
                    instance.TimeOut();
                    Interlocked.Increment(ref _totalFailed);
                    return;
                }

                var metadata = BuildMetadata(instance, currentStep, priority);
                var stepInstance = instance.GetOrCreateStep(currentStep.StepId, currentStep.Name);

                var success = await ExecuteStepWithRetryAsync(
                    instance, def, currentStep, stepInstance, metadata, ct);

                if (!success)
                {
                    // Compensation
                    if (def.CompensationPolicy?.Enabled == true &&
                        currentStep.CompensationStepId != null)
                    {
                        instance.StartCompensation();
                        var compStep = def.GetStep(currentStep.CompensationStepId);
                        if (compStep != null)
                            await RunWorkflowAsync(instance, def, compStep, 0, ct); // Critical priority
                    }

                    instance.Fail(stepInstance.ErrorMessage ?? "Step failed");
                    Interlocked.Increment(ref _totalFailed);
                    return;
                }

                // Advance to next step
                if (currentStep.NextStepId == null)
                {
                    instance.Complete();
                    Interlocked.Increment(ref _totalCompleted);
                    return;
                }

                var nextStep = def.GetStep(currentStep.NextStepId);
                if (nextStep == null)
                {
                    instance.Fail($"Next step '{currentStep.NextStepId}' not found");
                    Interlocked.Increment(ref _totalFailed);
                    return;
                }

                instance.AdvanceTo(nextStep.StepId);
                currentStep = nextStep;
            }
        }

        private async Task<bool> ExecuteStepWithRetryAsync(
            WorkflowInstance instance,
            WorkflowDefinition def,
            WorkflowStep step,
            WorkflowStepInstance stepInstance,
            MessageMetadata metadata,
            CancellationToken ct)
        {
            var retry = step.EffectiveRetry;
            var executor = FindExecutor(step.CapabilityRequired);

            if (executor == null)
            {
                // ส่งไป DLQ ทันที — ไม่มี executor รองรับ capability นี้
                _dlq.Enqueue(new DeadLetterMessage(
                    metadata,
                    $"Step={step.StepId}",
                    $"No executor for capability={step.CapabilityRequired}",
                    DateTime.UtcNow,
                    0));
                stepInstance.Fail($"No executor for {step.CapabilityRequired}");
                return false;
            }

            for (int attempt = 1; attempt <= retry.MaxAttempts; attempt++)
            {
                if (attempt > 1)
                {
                    var delay = retry.GetDelay(attempt - 1);
                    await Task.Delay(delay, ct);
                    stepInstance.MarkRetrying();
                }

                stepInstance.Start();

                using var stepCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                stepCts.CancelAfter(step.EffectiveTimeout);

                try
                {
                    var result = await executor.ExecuteAsync(step, instance, metadata, stepCts.Token);
                    stepInstance.Complete(result);
                    return true;
                }
                catch (OperationCanceledException) when (stepCts.IsCancellationRequested && !ct.IsCancellationRequested)
                {
                    stepInstance.Fail($"Step timed out after {step.EffectiveTimeout.TotalSeconds}s");
                    if (attempt == retry.MaxAttempts)
                    {
                        SendToDeadLetter(metadata, step, stepInstance, attempt, null);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    stepInstance.Fail(ex.Message);
                    if (attempt == retry.MaxAttempts)
                    {
                        SendToDeadLetter(metadata, step, stepInstance, attempt, ex);
                        return false;
                    }
                }
            }

            return false;
        }

        private void SendToDeadLetter(
            MessageMetadata metadata,
            WorkflowStep step,
            WorkflowStepInstance stepInstance,
            int totalAttempts,
            Exception? ex)
        {
            _dlq.Enqueue(new DeadLetterMessage(
                metadata,
                $"Step={step.StepId} Capability={step.CapabilityRequired}",
                stepInstance.ErrorMessage ?? "Unknown error",
                DateTime.UtcNow,
                totalAttempts,
                ex));
        }

        private IAgentExecutor? FindExecutor(CapabilityType capability)
        {
            if (!_executors.TryGetValue(capability, out var list)) return null;
            lock (list)
            {
                return list.Count == 0 ? null : list[0]; // Round-robin ขยายได้ภายหลัง
            }
        }

        private static MessageMetadata BuildMetadata(
            WorkflowInstance instance,
            WorkflowStep step,
            int priority) =>
            new(
                MessageId: $"MSG-{Guid.NewGuid():N}"[..16].ToUpper(),
                CorrelationId: instance.CorrelationId,
                CausationId: instance.WorkflowId,
                WorkflowId: instance.WorkflowId,
                TenantId: instance.TenantId,
                Priority: priority,
                DeadlineUtc: instance.DeadlineUtc ?? DateTime.UtcNow.AddMinutes(5),
                StepId: step.StepId
            );

        // ── Query ─────────────────────────────────────────────

        public WorkflowInstance? GetInstance(string workflowId) =>
            _instances.TryGetValue(workflowId, out var inst) ? inst : null;

        public IReadOnlyList<WorkflowInstance> GetInstancesByState(WorkflowState state) =>
            _instances.Values.Where(i => i.State == state).ToList();

        public WorkflowEngineStats GetStats() => new(
            TotalInstances: _instances.Count,
            ActiveInstances: ActiveInstances,
            TotalCompleted: _totalCompleted,
            TotalFailed: _totalFailed,
            DeadLetterCount: _dlq.Count,
            RegisteredDefinitions: _definitions.Count,
            RegisteredExecutors: _executors.Values.Sum(l => l.Count)
        );
    }

    public record WorkflowEngineStats(
        int TotalInstances,
        int ActiveInstances,
        long TotalCompleted,
        long TotalFailed,
        int DeadLetterCount,
        int RegisteredDefinitions,
        int RegisteredExecutors
    );

    // ──────────────────────────────────────────────────────────
    // DEMO EXECUTORS — สำหรับทดสอบ
    // ──────────────────────────────────────────────────────────

    public class MockExecutor : IAgentExecutor
    {
        private readonly CapabilityType _capability;
        private readonly bool _shouldFail;
        private readonly int _failOnAttempt;
        private int _callCount = 0;

        public CapabilityType Capability => _capability;
        public string AgentId { get; }
        public int CallCount => _callCount;

        public MockExecutor(
            string agentId,
            CapabilityType capability,
            bool shouldFail = false,
            int failOnAttempt = 0)
        {
            AgentId = agentId;
            _capability = capability;
            _shouldFail = shouldFail;
            _failOnAttempt = failOnAttempt;
        }

        public async Task<object?> ExecuteAsync(
            WorkflowStep step,
            WorkflowInstance workflow,
            MessageMetadata metadata,
            CancellationToken ct)
        {
            var attempt = Interlocked.Increment(ref _callCount);
            await Task.Delay(10, ct); // simulate work

            if (_shouldFail && (_failOnAttempt == 0 || attempt == _failOnAttempt))
                throw new InvalidOperationException($"[{AgentId}] Simulated failure on attempt {attempt}");

            return new { AgentId, StepId = step.StepId, Attempt = attempt, WorkflowId = workflow.WorkflowId };
        }
    }

    public class TimeoutExecutor : IAgentExecutor
    {
        public CapabilityType Capability => CapabilityType.Monitoring;
        public string AgentId => "TIMEOUT-AGENT";

        public async Task<object?> ExecuteAsync(
            WorkflowStep step,
            WorkflowInstance workflow,
            MessageMetadata metadata,
            CancellationToken ct)
        {
            await Task.Delay(TimeSpan.FromSeconds(60), ct); // จงใจ timeout
            return null;
        }
    }
}