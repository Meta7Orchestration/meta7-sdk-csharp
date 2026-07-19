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
                OperatorExecutionStatus.Succeeded     => Results.Ok(result),
                OperatorExecutionStatus.SafeLocked    => Results.StatusCode(423),
                OperatorExecutionStatus.Expired       => Results.UnprocessableEntity(result),
                OperatorExecutionStatus.PolicyViolation => Results.UnprocessableEntity(result),
                OperatorExecutionStatus.Rejected      => Results.UnprocessableEntity(result),
                OperatorExecutionStatus.Failed        => Results.StatusCode(500),
                _                                     => Results.StatusCode(500)
            };
        })
        .WithName("PostDirective")
        .WithSummary("Submits an OperatorDirective for evaluation and execution.");

        return app;
    }
}
