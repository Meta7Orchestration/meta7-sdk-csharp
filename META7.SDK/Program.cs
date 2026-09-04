// ═══════════════════════════════════════════════════════════════
// META7 Captain M7A SDK — Web API Entry Point
// จดจำไว้ แล้วไปด้วยกัน
// ═══════════════════════════════════════════════════════════════

using META7.SDK.Services;
using META7.SDK.Models;

var builder = WebApplication.CreateBuilder(args);

// ── Services ──────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "META7 Captain M7A SDK",
        Version = "v1",
        Description = "Deterministic Cognitive Runtime — META7 / QHCU / CAIM\n\nจดจำไว้ แล้วไปด้วยกัน"
    });
});
builder.Services.AddSingleton<CaptainM7AService>();
builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// ── Middleware ─────────────────────────────────────────────────
app.UseCors();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "META7 Captain M7A SDK v1");
    c.RoutePrefix = "swagger";
    c.DocumentTitle = "META7 SDK API";
});

// ── Root ───────────────────────────────────────────────────────
app.MapGet("/", () => new
{
    system = "META7 Captain M7A SDK",
    version = "1.0.0",
    status = "OPERATIONAL",
    layers = 30,
    motto = "จดจำไว้ แล้วไปด้วยกัน",
    endpoints = new[]
    {
        "GET  /",
        "GET  /meta7/captain/status",
        "POST /meta7/captain/execute",
        "GET  /meta7/captain/layers",
        "GET  /meta7/captain/health",
        "POST /meta7/saga/run",
        "POST /meta7/workflow/execute",
        "GET  /swagger"
    }
});

// ── Captain Endpoints ──────────────────────────────────────────
app.MapGet("/meta7/captain/status", (CaptainM7AService captain) =>
    captain.GetStatus());

app.MapGet("/meta7/captain/health", (CaptainM7AService captain) =>
    captain.GetHealth());

app.MapGet("/meta7/captain/layers", (CaptainM7AService captain) =>
    captain.GetLayerInfo());

app.MapPost("/meta7/captain/execute", (CaptainM7AService captain, CaptainRequest request) =>
    captain.Execute(request));

// ── Saga Endpoints ─────────────────────────────────────────────
app.MapPost("/meta7/saga/run", (CaptainM7AService captain, SagaRequest request) =>
    captain.RunSaga(request));

// ── Workflow Endpoints ─────────────────────────────────────────
app.MapPost("/meta7/workflow/execute", (CaptainM7AService captain, WorkflowRequest request) =>
    captain.ExecuteWorkflow(request));

// ── Controllers ────────────────────────────────────────────────
app.MapControllers();

// ── Port from Cloud Run ────────────────────────────────────────
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Run($"http://0.0.0.0:{port}");