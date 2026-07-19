// ══════════════════════════════════════════════════════════════════════════════
// META7 Operator API — startup
// Human Approval Layer: registers the approval pipeline as singleton services
// so that the in-memory queue is shared across all requests.
// ══════════════════════════════════════════════════════════════════════════════

using META7.Operator.Api.Approval;
using META7.Operator.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Services ──────────────────────────────────────────────────────────────────

builder.Services.AddControllers();

// Human Approval Layer — singleton so the in-memory queue survives requests.
builder.Services.AddSingleton<HumanApprovalQueue>();
builder.Services.AddSingleton<HumanApprovalGateway>(sp =>
    new HumanApprovalGateway(sp.GetRequiredService<HumanApprovalQueue>()));

// Execution pipeline — no-op default executor (real executor injected in prod).
builder.Services.AddSingleton<IRevenueActionExecutor, NoOpRevenueActionExecutor>();
builder.Services.AddSingleton<ISafeLockProvider, DefaultSafeLockProvider>();
builder.Services.AddSingleton<DirectiveExecutionService>();

// ── Pipeline ──────────────────────────────────────────────────────────────────

var app = builder.Build();

app.UseHttpsRedirection();
app.MapControllers();
app.Run();

// ── Default implementations (replaced in production via DI) ──────────────────

/// <summary>
/// Default SAFE_LOCK provider — reads the META7_SAFE_LOCK environment variable.
/// Set META7_SAFE_LOCK=true to engage SAFE_LOCK mode.
/// </summary>
internal sealed class DefaultSafeLockProvider : ISafeLockProvider
{
    public bool IsSafeLockActive =>
        string.Equals(
            Environment.GetEnvironmentVariable("META7_SAFE_LOCK"),
            "true",
            StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// No-op executor used when no real executor is registered.
/// Always returns true (simulates a successful write without side-effects).
/// Replace with a real implementation in production.
/// </summary>
internal sealed class NoOpRevenueActionExecutor : IRevenueActionExecutor
{
    public bool Execute(string requestId, META7.Operator.Contracts.Approval.OperatorActionType actionType, string payload)
        => true;
}

// Make Program accessible to the test project for WebApplicationFactory.
public partial class Program { }

