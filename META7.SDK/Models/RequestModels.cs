// ═══════════════════════════════════════════════════════════════
// META7 Captain M7A SDK — Request/Response Models
// ═══════════════════════════════════════════════════════════════

namespace META7.SDK.Models;

// ── Captain Execute ────────────────────────────────────────────
public record CaptainRequest(
    string Layer,
    string Command,
    string Payload,
    string? CorrelationId = null,
    int Priority = 1
);

public record CaptainResponse(
    bool Success,
    string Layer,
    string Command,
    string Result,
    string CorrelationId,
    long ExecutionTimeMs,
    string Timestamp
);

// ── Saga ───────────────────────────────────────────────────────
public record SagaRequest(
    string WorkflowId,
    string[] Steps,
    Dictionary<string, string>? Context = null,
    bool StopOnFailure = true
);

public record SagaResponse(
    string InstanceId,
    string WorkflowId,
    string State,
    int CompletedSteps,
    int TotalSteps,
    string[] Errors,
    string Timestamp
);

// ── Workflow ───────────────────────────────────────────────────
public record WorkflowRequest(
    string WorkflowType,
    string Payload,
    string? TenantId = null,
    string? CorrelationId = null
);

public record WorkflowResponse(
    string WorkflowId,
    string Status,
    string Result,
    string Timestamp
);