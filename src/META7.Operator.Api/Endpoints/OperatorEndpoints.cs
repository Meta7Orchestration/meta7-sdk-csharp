namespace META7.Operator.Api.Endpoints;

using META7.Operator.Api.Services;
using META7.Operator.Contracts;

internal static class OperatorEndpoints
{
    internal static IEndpointRouteBuilder MapOperatorEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
           .WithName("GetHealth")
           .WithSummary("Returns the health status of the Operator API.");

        app.MapPost("/v1/directives", async (
            OperatorDirective directive,
            DirectiveExecutionService service,
            CancellationToken ct) =>
        {
            var result = await service.ExecuteAsync(directive, ct);

            return result.Status switch
            {
                OperatorExecutionStatus.Succeeded       => Results.Ok(result),
                // 423 Locked (WebDAV, RFC 4918): resource is locked — used here to signal SAFE_LOCK active
                OperatorExecutionStatus.SafeLocked      => Results.Json(result, statusCode: 423),
                OperatorExecutionStatus.Expired         => Results.UnprocessableEntity(result),
                OperatorExecutionStatus.PolicyViolation => Results.UnprocessableEntity(result),
                OperatorExecutionStatus.Rejected        => Results.UnprocessableEntity(result),
                OperatorExecutionStatus.Failed          => Results.Json(result, statusCode: 500),
                _                                       => Results.Json(result, statusCode: 500)
            };
        })
        .WithName("PostDirective")
        .WithSummary("Submits an OperatorDirective for evaluation and execution.");

        return app;
    }
}
