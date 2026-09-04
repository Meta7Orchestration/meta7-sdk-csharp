// ═══════════════════════════════════════════════════════════════
// META7 Captain M7A SDK — REST Controller
// ═══════════════════════════════════════════════════════════════

using META7.SDK.Models;
using META7.SDK.Services;
using Microsoft.AspNetCore.Mvc;

namespace META7.SDK.Controllers;

[ApiController]
[Route("meta7/captain")]
[Produces("application/json")]
public class CaptainM7AController : ControllerBase
{
    private readonly CaptainM7AService _captain;

    public CaptainM7AController(CaptainM7AService captain) =>
        _captain = captain;

    /// <summary>Get system status</summary>
    [HttpGet("status")]
    public IActionResult GetStatus() => Ok(_captain.GetStatus());

    /// <summary>Get health check</summary>
    [HttpGet("health")]
    public IActionResult GetHealth() => Ok(_captain.GetHealth());

    /// <summary>Get layer information</summary>
    [HttpGet("layers")]
    public IActionResult GetLayers() => Ok(_captain.GetLayerInfo());

    /// <summary>Execute a command on a specific layer</summary>
    [HttpPost("execute")]
    public IActionResult Execute([FromBody] CaptainRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Layer))
            return BadRequest(new { error = "Layer is required" });
        if (string.IsNullOrWhiteSpace(request.Command))
            return BadRequest(new { error = "Command is required" });

        var result = _captain.Execute(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}

[ApiController]
[Route("meta7/saga")]
[Produces("application/json")]
public class SagaController : ControllerBase
{
    private readonly CaptainM7AService _captain;

    public SagaController(CaptainM7AService captain) =>
        _captain = captain;

    /// <summary>Run a Saga workflow</summary>
    [HttpPost("run")]
    public IActionResult RunSaga([FromBody] SagaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.WorkflowId))
            return BadRequest(new { error = "WorkflowId is required" });
        if (request.Steps == null || request.Steps.Length == 0)
            return BadRequest(new { error = "Steps are required" });

        return Ok(_captain.RunSaga(request));
    }
}

[ApiController]
[Route("meta7/workflow")]
[Produces("application/json")]
public class WorkflowController : ControllerBase
{
    private readonly CaptainM7AService _captain;

    public WorkflowController(CaptainM7AService captain) =>
        _captain = captain;

    /// <summary>Execute a workflow</summary>
    [HttpPost("execute")]
    public IActionResult ExecuteWorkflow([FromBody] WorkflowRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.WorkflowType))
            return BadRequest(new { error = "WorkflowType is required" });

        return Ok(_captain.ExecuteWorkflow(request));
    }
}