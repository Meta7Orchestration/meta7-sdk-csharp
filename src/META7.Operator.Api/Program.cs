using META7.Operator.Api;
using META7.Operator.Api.Execution;
using META7.Operator.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<OperatorExecutionOptions>(builder.Configuration.GetSection("OperatorExecution"));
builder.Services.AddSingleton<ISafeLockState, DefaultSafeLockState>();
builder.Services.AddScoped<IBrowserBootstrap, BrowserBootstrap>();
builder.Services.AddScoped<IOperatorExecutor, PlaywrightOperatorExecutor>();
builder.Services.AddScoped<DirectiveExecutionService>();

var app = builder.Build();

app.MapPost("/operator/execute", async (
    OperatorDirective directive,
    DirectiveExecutionService executionService,
    CancellationToken cancellationToken) =>
{
    var result = await executionService.ExecuteAsync(directive, cancellationToken);
    return result.Succeeded ? Results.Ok(result) : Results.BadRequest(result);
});

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();

public partial class Program;
