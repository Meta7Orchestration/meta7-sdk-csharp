using System.Text.Json;
using System.Text.Json.Serialization;
using META7.CaptainM7A;
using META7.Operator.Api.Endpoints;
using META7.Operator.Api.Execution;
using META7.Operator.Api.Policies;
using META7.Operator.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ── JSON serialisation ────────────────────────────────────────────────────────
builder.Services.ConfigureHttpJsonOptions(opts =>
{
    opts.SerializerOptions.PropertyNameCaseInsensitive = true;
    opts.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});

// ── Operator services ─────────────────────────────────────────────────────────
builder.Services.AddSingleton<OperatorPolicyGate>();
builder.Services.AddSingleton<IOperatorExecutor, ReadOnlyOperatorExecutor>();
builder.Services.AddSingleton<DirectiveExecutionService>();

// Default SAFE_LOCK reader: always unlocked.
// Override this registration in production or tests to wire in a live store.
builder.Services.AddSingleton<ISafeLockStateReader, DefaultUnlockedSafeLockReader>();

var app = builder.Build();

// ── Endpoints ─────────────────────────────────────────────────────────────────
app.MapOperatorEndpoints();

app.Run();

// ── Default SafeLock reader (always unlocked, suitable for development) ───────
internal sealed class DefaultUnlockedSafeLockReader : ISafeLockStateReader
{
    public bool IsSafeLockActive => false;
}

// Expose Program for WebApplicationFactory in integration tests
public partial class Program { }
