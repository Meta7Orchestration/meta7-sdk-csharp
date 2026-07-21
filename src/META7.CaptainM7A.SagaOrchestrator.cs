// ============================================================
// META7 Captain M7A — Saga Orchestrator
// Pattern: Orchestration-based Saga (not Choreography)
//
// Design Principles:
//   1. Forward transaction  = sequential step execution
//   2. Compensating tx      = reverse-order rollback chain
//   3. CausationId          = links every compensation event
//                             back to the original WorkflowId
//   4. Idempotency          = each step/compensation has
//                             idempotency key to prevent double-exec
//   5. Audit trail          = every forward + backward action logged
//
// Compensation Chain:
//   Forward:  A → B → C → D
//                         ↓ FAIL
//   Backward: C-comp ← B-comp ← A-comp
//             (CausationId = original WorkflowId throughout)
//
// Integration with WorkflowEngineV3:
//   SagaCoordinator wraps WorkflowEngineV3 and adds:
//   - SagaDefinition (steps with paired compensations)
//   - CompensationChain (ordered rollback registry)
//   - SagaInstance (tracks forward progress + compensation state)
//   - SagaAuditLog (full trace with CausationId linkage)
// ============================================================

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using META7.CaptainM7A.WorkflowEngineV3;

namespace META7.CaptainM7A.SagaOrchestration
{
    // ──────────────────────────────────────────────────────────
    // SAGA STATE MACHINE
    // ──────────────────────────────────────────────────────────

    public enum SagaState
    {
        Created,
        Running,           // forward transaction in progress
        Compensating,      // rollback chain executing
        Completed,         // all forward steps succeeded
        Compensated,       // rollback completed successfully
        Failed             // rollback itself failed (needs manual intervention)
    }

    public enum SagaStepState
    {
        Pending,
        Executing,
        Completed,
        Failed,
        Compensating,
        Compensated,
        CompensationFailed
    }

    // ──────────────────────────────────────────────────────────
    // SAGA DEFINITION — Declarative step + compensation pairs
    // ──────────────────────────────────────────────────────────

    public record SagaStepDefinition(
        string StepId,
        string Name,
        CapabilityType ForwardCapability,
        CapabilityType? CompensationCapability,  // null = no compensation needed
        string? CompensationStepId,              // explicit compensation step ID
        WorkflowEngineV3.RetryPolicy? ForwardRetry = null,
        WorkflowEngineV3.RetryPolicy? CompensationRetry = null,
        TimeSpan? ForwardTimeout = null,
        TimeSpan? CompensationTimeout = null,
        Dictionary<string, object>? Parameters = null
    )
    {
        public bool HasCompensation =>
            CompensationCapability.HasValue || CompensationStepId != null;

        public WorkflowEngineV3.RetryPolicy EffectiveForwardRetry =>
            ForwardRetry ?? WorkflowEngineV3.RetryPolicy.Default;

        public WorkflowEngineV3.RetryPolicy EffectiveCompensationRetry =>
            CompensationRetry ?? new WorkflowEngineV3.RetryPolicy(2, TimeSpan.FromMilliseconds(50));

        public TimeSpan EffectiveForwardTimeout =>
            ForwardTimeout ?? TimeSpan.FromSeconds(30);

        public TimeSpan EffectiveCompensationTimeout =>
            CompensationTimeout ?? TimeSpan.FromSeconds(15);
    }

    public record SagaDefinition(
        string SagaId,
        string Name,
        string Version,
        IReadOnlyList<SagaStepDefinition> Steps,  // ordered: first → last
        TimeSpan? GlobalTimeout = null,
        bool StopCompensationOnFailure = false     // true = halt rollback on comp failure
    )
    {
        // Compensation order = reverse of forward order
        // Only steps that have compensation and were completed
        public IReadOnlyList<SagaStepDefinition> CompensationOrder =>
            Steps.Where(s => s.HasCompensation).Reverse().ToList();
    }

    // ──────────────────────────────────────────────────────────
    // SAGA AUDIT LOG — Full trace with CausationId linkage
    // ──────────────────────────────────────────────────────────

    public enum SagaAuditEventType
    {
        SagaStarted,
        StepStarted,
        StepCompleted,
        StepFailed,
        StepRetrying,
        CompensationStarted,
        CompensationStepStarted,
        CompensationStepCompleted,
        CompensationStepFailed,
        SagaCompleted,
        SagaCompensated,
        SagaFailed
    }

    public record SagaAuditEvent(
        string EventId,
        string SagaInstanceId,
        string CorrelationId,
        string CausationId,         // links compensation events back to original saga
        SagaAuditEventType Type,
        string? StepId,
        string Message,
        DateTime OccurredAt,
        string? ErrorDetail = null,
        object? Payload = null
    );

    public class SagaAuditLog
    {
        private readonly ConcurrentQueue<SagaAuditEvent> _events = new();
        private long _seq = 0;

        public void Record(
            string sagaInstanceId,
            string correlationId,
            string causationId,
            SagaAuditEventType type,
            string? stepId,
            string message,
            string? error = null,
            object? payload = null)
        {
            Interlocked.Increment(ref _seq);
            _events.Enqueue(new SagaAuditEvent(
                EventId: $"SAE-{_seq:D6}",
                SagaInstanceId: sagaInstanceId,
                CorrelationId: correlationId,
                CausationId: causationId,
                Type: type,
                StepId: stepId,
                Message: message,
                OccurredAt: DateTime.UtcNow,
                ErrorDetail: error,
                Payload: payload));
        }

        public IReadOnlyList<SagaAuditEvent> GetAll() => _events.ToArray();

        public IReadOnlyList<SagaAuditEvent> GetBySaga(string sagaInstanceId) =>
            _events.Where(e => e.SagaInstanceId == sagaInstanceId).ToArray();

        public IReadOnlyList<SagaAuditEvent> GetCompensationChain(string originalSagaId) =>
            _events.Where(e =>
                e.CausationId == originalSagaId &&
                e.Type is SagaAuditEventType.CompensationStepStarted
                       or SagaAuditEventType.CompensationStepCompleted
                       or SagaAuditEventType.CompensationStepFailed)
            .ToArray();
    }

    // ──────────────────────────────────────────────────────────
    // SAGA STEP INSTANCE — Runtime state per step
    // ──────────────────────────────────────────────────────────

    public class SagaStepInstance
    {
        public string StepId { get; }
        public string Name { get; }
        public SagaStepState State { get; private set; } = SagaStepState.Pending;
        public int ForwardAttempts { get; private set; } = 0;
        public int CompensationAttempts { get; private set; } = 0;
        public DateTime? ForwardStartedAt { get; private set; }
        public DateTime? ForwardCompletedAt { get; private set; }
        public DateTime? CompensationStartedAt { get; private set; }
        public DateTime? CompensationCompletedAt { get; private set; }
        public string? ForwardError { get; private set; }
        public string? CompensationError { get; private set; }
        public object? ForwardResult { get; private set; }
        public object? CompensationResult { get; private set; }
        public string IdempotencyKey { get; }

        public SagaStepInstance(string stepId, string name, string sagaInstanceId)
        {
            StepId = stepId;
            Name = name;
            // Idempotency key = saga + step — prevents double execution on retry
            IdempotencyKey = $"{sagaInstanceId}:{stepId}";
        }

        public void StartForward()
        {
            State = SagaStepState.Executing;
            ForwardStartedAt = DateTime.UtcNow;
            ForwardAttempts++;
        }

        public void CompleteForward(object? result = null)
        {
            State = SagaStepState.Completed;
            ForwardCompletedAt = DateTime.UtcNow;
            ForwardResult = result;
        }

        public void FailForward(string error)
        {
            State = SagaStepState.Failed;
            ForwardError = error;
        }

        public void StartCompensation()
        {
            State = SagaStepState.Compensating;
            CompensationStartedAt = DateTime.UtcNow;
            CompensationAttempts++;
        }

        public void CompleteCompensation(object? result = null)
        {
            State = SagaStepState.Compensated;
            CompensationCompletedAt = DateTime.UtcNow;
            CompensationResult = result;
        }

        public void FailCompensation(string error)
        {
            State = SagaStepState.CompensationFailed;
            CompensationError = error;
        }
    }

    // ──────────────────────────────────────────────────────────
    // SAGA INSTANCE — Full runtime state
    // ──────────────────────────────────────────────────────────

    public class SagaInstance
    {
        public string SagaInstanceId { get; }
        public string SagaDefinitionId { get; }
        public string CorrelationId { get; }
        public string? TenantId { get; }
        public SagaState State { get; private set; } = SagaState.Created;
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
        public DateTime? StartedAt { get; private set; }
        public DateTime? CompletedAt { get; private set; }
        public DateTime? DeadlineUtc { get; }
        public string? FailedAtStepId { get; private set; }
        public string? ErrorMessage { get; private set; }
        private int _compensatedSteps = 0;
        private int _compensationFailures = 0;
        public int CompensatedSteps => _compensatedSteps;
        public int CompensationFailures => _compensationFailures;

        private readonly Dictionary<string, SagaStepInstance> _steps = new();
        private readonly object _lock = new();

        public IReadOnlyDictionary<string, SagaStepInstance> Steps => _steps;

        // Completed forward steps in execution order (for compensation ordering)
        private readonly List<string> _completedStepOrder = new();

        public IReadOnlyList<string> CompletedStepOrder => _completedStepOrder;

        public bool IsTimedOut =>
            DeadlineUtc.HasValue && DateTime.UtcNow > DeadlineUtc.Value;

        public SagaInstance(
            string sagaInstanceId,
            string sagaDefinitionId,
            string correlationId,
            string? tenantId = null,
            TimeSpan? timeout = null)
        {
            SagaInstanceId = sagaInstanceId;
            SagaDefinitionId = sagaDefinitionId;
            CorrelationId = correlationId;
            TenantId = tenantId;
            DeadlineUtc = timeout.HasValue ? DateTime.UtcNow + timeout.Value : null;
        }

        public void Start()
        {
            lock (_lock)
            {
                State = SagaState.Running;
                StartedAt = DateTime.UtcNow;
            }
        }

        public SagaStepInstance GetOrCreateStep(string stepId, string name)
        {
            lock (_lock)
            {
                if (!_steps.TryGetValue(stepId, out var step))
                {
                    step = new SagaStepInstance(stepId, name, SagaInstanceId);
                    _steps[stepId] = step;
                }
                return step;
            }
        }

        public void RecordStepCompleted(string stepId)
        {
            lock (_lock)
            {
                if (!_completedStepOrder.Contains(stepId))
                    _completedStepOrder.Add(stepId);
            }
        }

        public void StartCompensation(string failedStepId, string error)
        {
            lock (_lock)
            {
                State = SagaState.Compensating;
                FailedAtStepId = failedStepId;
                ErrorMessage = error;
            }
        }

        public void RecordCompensationCompleted() =>
            Interlocked.Increment(ref _compensatedSteps);

        public void RecordCompensationFailed() =>
            Interlocked.Increment(ref _compensationFailures);

        public void Complete()
        {
            lock (_lock)
            {
                State = SagaState.Completed;
                CompletedAt = DateTime.UtcNow;
            }
        }

        public void MarkCompensated()
        {
            lock (_lock)
            {
                State = SagaState.Compensated;
                CompletedAt = DateTime.UtcNow;
            }
        }

        public void MarkFailed(string error)
        {
            lock (_lock)
            {
                State = SagaState.Failed;
                ErrorMessage = error;
                CompletedAt = DateTime.UtcNow;
            }
        }
    }

    // ──────────────────────────────────────────────────────────
    // SAGA EXECUTOR INTERFACE
    // Separate from IAgentExecutor to carry saga context
    // ──────────────────────────────────────────────────────────

    public interface ISagaStepExecutor
    {
        CapabilityType Capability { get; }
        string ExecutorId { get; }

        Task<object?> ExecuteForwardAsync(
            SagaStepDefinition step,
            SagaInstance saga,
            MessageMetadata metadata,
            CancellationToken ct);

        Task<object?> ExecuteCompensationAsync(
            SagaStepDefinition step,
            SagaInstance saga,
            MessageMetadata metadata,
            object? forwardResult,   // result from forward step (for undo context)
            CancellationToken ct);
    }

    // ──────────────────────────────────────────────────────────
    // SAGA COORDINATOR — The Orchestrator
    // ──────────────────────────────────────────────────────────

    public class SagaCoordinator
    {
        private readonly ConcurrentDictionary<string, SagaDefinition> _definitions = new();
        private readonly ConcurrentDictionary<string, SagaInstance> _instances = new();
        private readonly ConcurrentDictionary<CapabilityType, ISagaStepExecutor> _executors = new();
        private readonly SagaAuditLog _auditLog = new();
        private long _totalCompleted = 0;
        private long _totalCompensated = 0;
        private long _totalFailed = 0;

        public SagaAuditLog AuditLog => _auditLog;
        public long TotalCompleted => _totalCompleted;
        public long TotalCompensated => _totalCompensated;
        public long TotalFailed => _totalFailed;

        // ── Registration ──────────────────────────────────────

        public void RegisterDefinition(SagaDefinition def) =>
            _definitions[def.SagaId] = def;

        public void RegisterExecutor(ISagaStepExecutor executor) =>
            _executors[executor.Capability] = executor;

        // ── Saga Lifecycle ────────────────────────────────────

        public SagaInstance CreateInstance(
            string sagaDefinitionId,
            string? correlationId = null,
            string? tenantId = null,
            TimeSpan? timeout = null)
        {
            if (!_definitions.ContainsKey(sagaDefinitionId))
                throw new InvalidOperationException($"Saga definition '{sagaDefinitionId}' not found");

            var instanceId = $"SAGA-{Guid.NewGuid():N}"[..20].ToUpper();
            var corrId = correlationId ?? $"CORR-{Guid.NewGuid():N}"[..12].ToUpper();

            var instance = new SagaInstance(instanceId, sagaDefinitionId, corrId, tenantId, timeout);
            _instances[instanceId] = instance;
            return instance;
        }

        public async Task<SagaInstance> ExecuteAsync(
            string sagaInstanceId,
            int priority = 2,
            CancellationToken ct = default)
        {
            if (!_instances.TryGetValue(sagaInstanceId, out var instance))
                throw new InvalidOperationException($"Saga instance '{sagaInstanceId}' not found");

            if (!_definitions.TryGetValue(instance.SagaDefinitionId, out var def))
                throw new InvalidOperationException($"Saga definition '{instance.SagaDefinitionId}' not found");

            instance.Start();
            _auditLog.Record(
                instance.SagaInstanceId, instance.CorrelationId,
                instance.SagaInstanceId,  // CausationId = self for saga start
                SagaAuditEventType.SagaStarted, null,
                $"Saga '{def.Name}' started with {def.Steps.Count} steps");

            // ── FORWARD TRANSACTION ───────────────────────────
            foreach (var stepDef in def.Steps)
            {
                if (instance.IsTimedOut)
                {
                    await RunCompensationChainAsync(instance, def, priority, ct);
                    instance.MarkFailed("Saga timed out");
                    Interlocked.Increment(ref _totalFailed);
                    return instance;
                }

                var stepInstance = instance.GetOrCreateStep(stepDef.StepId, stepDef.Name);
                var metadata = BuildMetadata(instance, stepDef, priority);

                _auditLog.Record(
                    instance.SagaInstanceId, instance.CorrelationId,
                    instance.SagaInstanceId,
                    SagaAuditEventType.StepStarted, stepDef.StepId,
                    $"Forward step '{stepDef.Name}' starting");

                var success = await ExecuteForwardStepAsync(
                    instance, def, stepDef, stepInstance, metadata, ct);

                if (success)
                {
                    instance.RecordStepCompleted(stepDef.StepId);
                    _auditLog.Record(
                        instance.SagaInstanceId, instance.CorrelationId,
                        instance.SagaInstanceId,
                        SagaAuditEventType.StepCompleted, stepDef.StepId,
                        $"Forward step '{stepDef.Name}' completed",
                        payload: stepInstance.ForwardResult);
                }
                else
                {
                    // ── TRIGGER COMPENSATION CHAIN ────────────
                    _auditLog.Record(
                        instance.SagaInstanceId, instance.CorrelationId,
                        instance.SagaInstanceId,
                        SagaAuditEventType.StepFailed, stepDef.StepId,
                        $"Forward step '{stepDef.Name}' failed — triggering compensation",
                        error: stepInstance.ForwardError);

                    instance.StartCompensation(stepDef.StepId, stepInstance.ForwardError ?? "Unknown");

                    _auditLog.Record(
                        instance.SagaInstanceId, instance.CorrelationId,
                        instance.SagaInstanceId,
                        SagaAuditEventType.CompensationStarted, null,
                        $"Compensation chain starting — will rollback {instance.CompletedStepOrder.Count} completed steps");

                    await RunCompensationChainAsync(instance, def, priority, ct);

                    if (instance.CompensationFailures == 0)
                    {
                        instance.MarkCompensated();
                        Interlocked.Increment(ref _totalCompensated);
                        _auditLog.Record(
                            instance.SagaInstanceId, instance.CorrelationId,
                            instance.SagaInstanceId,
                            SagaAuditEventType.SagaCompensated, null,
                            $"Saga fully compensated — {instance.CompensatedSteps} steps rolled back");
                    }
                    else
                    {
                        instance.MarkFailed(
                            $"Compensation failed on {instance.CompensationFailures} step(s)");
                        Interlocked.Increment(ref _totalFailed);
                        _auditLog.Record(
                            instance.SagaInstanceId, instance.CorrelationId,
                            instance.SagaInstanceId,
                            SagaAuditEventType.SagaFailed, null,
                            $"Saga failed — {instance.CompensationFailures} compensation failures require manual intervention");
                    }

                    return instance;
                }
            }

            // All forward steps succeeded
            instance.Complete();
            Interlocked.Increment(ref _totalCompleted);
            _auditLog.Record(
                instance.SagaInstanceId, instance.CorrelationId,
                instance.SagaInstanceId,
                SagaAuditEventType.SagaCompleted, null,
                $"Saga '{def.Name}' completed successfully — all {def.Steps.Count} steps passed");

            return instance;
        }

        // ── Compensation Chain ────────────────────────────────
        // Executes in REVERSE order of completed steps
        // CausationId = original SagaInstanceId throughout

        private async Task RunCompensationChainAsync(
            SagaInstance instance,
            SagaDefinition def,
            int priority,
            CancellationToken ct)
        {
            // Reverse the completed steps to get rollback order
            var rollbackOrder = instance.CompletedStepOrder
                .Select(stepId => def.Steps.FirstOrDefault(s => s.StepId == stepId))
                .Where(s => s != null && s!.HasCompensation)
                .Reverse()
                .ToList();

            foreach (var stepDef in rollbackOrder)
            {
                if (stepDef == null) continue;

                if (ct.IsCancellationRequested) break;

                var stepInstance = instance.GetOrCreateStep(stepDef.StepId, stepDef.Name);

                // CausationId links compensation back to original saga
                var metadata = BuildCompensationMetadata(instance, stepDef, priority);

                _auditLog.Record(
                    instance.SagaInstanceId, instance.CorrelationId,
                    instance.SagaInstanceId,  // CausationId = original saga
                    SagaAuditEventType.CompensationStepStarted, stepDef.StepId,
                    $"Compensating step '{stepDef.Name}' (idempotency={stepInstance.IdempotencyKey})");

                stepInstance.StartCompensation();

                var compSuccess = await ExecuteCompensationStepAsync(
                    instance, stepDef, stepInstance, metadata, ct);

                if (compSuccess)
                {
                    instance.RecordCompensationCompleted();
                    _auditLog.Record(
                        instance.SagaInstanceId, instance.CorrelationId,
                        instance.SagaInstanceId,
                        SagaAuditEventType.CompensationStepCompleted, stepDef.StepId,
                        $"Compensation of '{stepDef.Name}' succeeded",
                        payload: stepInstance.CompensationResult);
                }
                else
                {
                    instance.RecordCompensationFailed();
                    _auditLog.Record(
                        instance.SagaInstanceId, instance.CorrelationId,
                        instance.SagaInstanceId,
                        SagaAuditEventType.CompensationStepFailed, stepDef.StepId,
                        $"Compensation of '{stepDef.Name}' FAILED — manual intervention required",
                        error: stepInstance.CompensationError);

                    if (def.StopCompensationOnFailure) break;
                }
            }
        }

        // ── Step Execution with Retry ─────────────────────────

        private async Task<bool> ExecuteForwardStepAsync(
            SagaInstance instance,
            SagaDefinition def,
            SagaStepDefinition stepDef,
            SagaStepInstance stepInstance,
            MessageMetadata metadata,
            CancellationToken ct)
        {
            var executor = FindExecutor(stepDef.ForwardCapability);
            if (executor == null)
            {
                stepInstance.FailForward($"No executor for capability={stepDef.ForwardCapability}");
                return false;
            }

            var retry = stepDef.EffectiveForwardRetry;

            for (int attempt = 1; attempt <= retry.MaxAttempts; attempt++)
            {
                if (attempt > 1)
                {
                    await Task.Delay(retry.GetDelay(attempt - 1), ct);
                    _auditLog.Record(
                        instance.SagaInstanceId, instance.CorrelationId,
                        instance.SagaInstanceId,
                        SagaAuditEventType.StepRetrying, stepDef.StepId,
                        $"Retrying '{stepDef.Name}' attempt {attempt}/{retry.MaxAttempts}");
                }

                stepInstance.StartForward();

                using var stepCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                stepCts.CancelAfter(stepDef.EffectiveForwardTimeout);

                try
                {
                    var result = await executor.ExecuteForwardAsync(stepDef, instance, metadata, stepCts.Token);
                    stepInstance.CompleteForward(result);
                    return true;
                }
                catch (OperationCanceledException) when (stepCts.IsCancellationRequested && !ct.IsCancellationRequested)
                {
                    stepInstance.FailForward($"Step timed out after {stepDef.EffectiveForwardTimeout.TotalSeconds}s");
                }
                catch (Exception ex)
                {
                    stepInstance.FailForward(ex.Message);
                }
            }

            return false;
        }

        private async Task<bool> ExecuteCompensationStepAsync(
            SagaInstance instance,
            SagaStepDefinition stepDef,
            SagaStepInstance stepInstance,
            MessageMetadata metadata,
            CancellationToken ct)
        {
            var capability = stepDef.CompensationCapability ?? stepDef.ForwardCapability;
            var executor = FindExecutor(capability);

            if (executor == null)
            {
                stepInstance.FailCompensation($"No executor for compensation capability={capability}");
                return false;
            }

            var retry = stepDef.EffectiveCompensationRetry;

            for (int attempt = 1; attempt <= retry.MaxAttempts; attempt++)
            {
                if (attempt > 1)
                    await Task.Delay(retry.GetDelay(attempt - 1), ct);

                stepInstance.StartCompensation();

                using var stepCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                stepCts.CancelAfter(stepDef.EffectiveCompensationTimeout);

                try
                {
                    var result = await executor.ExecuteCompensationAsync(
                        stepDef, instance, metadata,
                        stepInstance.ForwardResult,  // pass forward result for undo context
                        stepCts.Token);
                    stepInstance.CompleteCompensation(result);
                    return true;
                }
                catch (OperationCanceledException) when (stepCts.IsCancellationRequested && !ct.IsCancellationRequested)
                {
                    stepInstance.FailCompensation($"Compensation timed out after {stepDef.EffectiveCompensationTimeout.TotalSeconds}s");
                }
                catch (Exception ex)
                {
                    stepInstance.FailCompensation(ex.Message);
                }
            }

            return false;
        }

        // ── Helpers ───────────────────────────────────────────

        private ISagaStepExecutor? FindExecutor(CapabilityType capability) =>
            _executors.TryGetValue(capability, out var exec) ? exec : null;

        private static MessageMetadata BuildMetadata(
            SagaInstance instance,
            SagaStepDefinition step,
            int priority) =>
            new(
                MessageId: $"MSG-{Guid.NewGuid():N}"[..16].ToUpper(),
                CorrelationId: instance.CorrelationId,
                CausationId: instance.SagaInstanceId,
                WorkflowId: instance.SagaInstanceId,
                TenantId: instance.TenantId,
                Priority: priority,
                DeadlineUtc: instance.DeadlineUtc ?? DateTime.UtcNow.AddMinutes(5),
                StepId: step.StepId
            );

        private static MessageMetadata BuildCompensationMetadata(
            SagaInstance instance,
            SagaStepDefinition step,
            int priority) =>
            new(
                MessageId: $"COMP-{Guid.NewGuid():N}"[..16].ToUpper(),
                CorrelationId: instance.CorrelationId,
                // CausationId = original SagaInstanceId — this is the key linkage
                CausationId: instance.SagaInstanceId,
                WorkflowId: instance.SagaInstanceId,
                TenantId: instance.TenantId,
                Priority: 0,  // compensation always runs at Critical priority
                DeadlineUtc: DateTime.UtcNow.AddMinutes(2),
                StepId: $"COMP-{step.StepId}"
            );

        // ── Query ─────────────────────────────────────────────

        public SagaInstance? GetInstance(string sagaInstanceId) =>
            _instances.TryGetValue(sagaInstanceId, out var inst) ? inst : null;

        public IReadOnlyList<SagaInstance> GetByState(SagaState state) =>
            _instances.Values.Where(i => i.State == state).ToList();

        public SagaCoordinatorStats GetStats() => new(
            TotalInstances: _instances.Count,
            TotalCompleted: _totalCompleted,
            TotalCompensated: _totalCompensated,
            TotalFailed: _totalFailed,
            RegisteredDefinitions: _definitions.Count,
            RegisteredExecutors: _executors.Count,
            TotalAuditEvents: _auditLog.GetAll().Count
        );
    }

    public record SagaCoordinatorStats(
        int TotalInstances,
        long TotalCompleted,
        long TotalCompensated,
        long TotalFailed,
        int RegisteredDefinitions,
        int RegisteredExecutors,
        int TotalAuditEvents
    );

    // ──────────────────────────────────────────────────────────
    // MOCK SAGA EXECUTORS — สำหรับ Demo และ Testing
    // ──────────────────────────────────────────────────────────

    public class MockSagaExecutor : ISagaStepExecutor
    {
        private readonly bool _forwardFails;
        private readonly bool _compensationFails;
        private int _forwardCallCount;
        private int _compensationCallCount;

        public CapabilityType Capability { get; }
        public string ExecutorId { get; }
        public int ForwardCallCount => _forwardCallCount;
        public int CompensationCallCount => _compensationCallCount;

        public MockSagaExecutor(
            string executorId,
            CapabilityType capability,
            bool forwardFails = false,
            bool compensationFails = false)
        {
            ExecutorId = executorId;
            Capability = capability;
            _forwardFails = forwardFails;
            _compensationFails = compensationFails;
        }

        public async Task<object?> ExecuteForwardAsync(
            SagaStepDefinition step,
            SagaInstance saga,
            MessageMetadata metadata,
            CancellationToken ct)
        {
            Interlocked.Increment(ref _forwardCallCount);
            await Task.Delay(5, ct);

            if (_forwardFails)
                throw new InvalidOperationException(
                    $"[{ExecutorId}] Forward execution failed for step={step.StepId}");

            return new
            {
                ExecutorId,
                StepId = step.StepId,
                SagaId = saga.SagaInstanceId,
                IdempotencyKey = $"{saga.SagaInstanceId}:{step.StepId}",
                ExecutedAt = DateTime.UtcNow
            };
        }

        public async Task<object?> ExecuteCompensationAsync(
            SagaStepDefinition step,
            SagaInstance saga,
            MessageMetadata metadata,
            object? forwardResult,
            CancellationToken ct)
        {
            Interlocked.Increment(ref _compensationCallCount);
            await Task.Delay(5, ct);

            if (_compensationFails)
                throw new InvalidOperationException(
                    $"[{ExecutorId}] Compensation failed for step={step.StepId}");

            return new
            {
                ExecutorId,
                StepId = step.StepId,
                Action = "COMPENSATED",
                OriginalResult = forwardResult,
                CausationId = metadata.CausationId,
                CompensatedAt = DateTime.UtcNow
            };
        }
    }

    // Executor ที่ fail เฉพาะ forward ครั้งแรก (simulate transient failure)
    public class TransientFailSagaExecutor : ISagaStepExecutor
    {
        private int _callCount = 0;
        private readonly int _failUntilAttempt;

        public CapabilityType Capability { get; }
        public string ExecutorId { get; }

        public TransientFailSagaExecutor(
            string executorId,
            CapabilityType capability,
            int failUntilAttempt = 1)
        {
            ExecutorId = executorId;
            Capability = capability;
            _failUntilAttempt = failUntilAttempt;
        }

        public async Task<object?> ExecuteForwardAsync(
            SagaStepDefinition step,
            SagaInstance saga,
            MessageMetadata metadata,
            CancellationToken ct)
        {
            var attempt = Interlocked.Increment(ref _callCount);
            await Task.Delay(5, ct);

            if (attempt <= _failUntilAttempt)
                throw new InvalidOperationException(
                    $"[{ExecutorId}] Transient failure on attempt {attempt}");

            return new { ExecutorId, Attempt = attempt, StepId = step.StepId };
        }

        public async Task<object?> ExecuteCompensationAsync(
            SagaStepDefinition step,
            SagaInstance saga,
            MessageMetadata metadata,
            object? forwardResult,
            CancellationToken ct)
        {
            await Task.Delay(5, ct);
            return new { ExecutorId, Action = "COMPENSATED", StepId = step.StepId };
        }
    }
}