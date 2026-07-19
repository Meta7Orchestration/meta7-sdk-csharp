// ══════════════════════════════════════════════════════════════════════════════
// META7 Operator API — Human Approval Controller
// Human Approval Layer: REST endpoints for human operators to review, approve,
// or reject pending write-action requests.
//
// Endpoints:
//   GET  /v1/approvals/pending        — list all pending requests
//   POST /v1/approvals/{id}/approve   — approve a specific request
//   POST /v1/approvals/{id}/reject    — reject a specific request
//
// Safety guarantees:
//   • No automated approval is possible — all transitions require an explicit
//     human-initiated HTTP call.
//   • Approved actions execute only through DirectiveExecutionService, which
//     re-checks SAFE_LOCK before calling the executor.
// ══════════════════════════════════════════════════════════════════════════════

using Microsoft.AspNetCore.Mvc;
using META7.Operator.Api.Services;
using META7.Operator.Contracts.Approval;

namespace META7.Operator.Api.Approval;

// ── Request body DTOs ─────────────────────────────────────────────────────────

/// <summary>Body for POST /v1/approvals/{id}/approve.</summary>
public sealed record ApproveActionRequest(
    /// <summary>Name or ID of the human operator performing the approval.</summary>
    string ApprovedBy);

/// <summary>Body for POST /v1/approvals/{id}/reject.</summary>
public sealed record RejectActionRequest(
    /// <summary>Optional human-readable reason for rejection.</summary>
    string? Reason = null);

// ── Controller ────────────────────────────────────────────────────────────────

/// <summary>
/// Provides human-operator endpoints for the META7 Human Approval Layer.
/// All state transitions require an explicit human-initiated call.
/// </summary>
[ApiController]
[Route("v1/approvals")]
public class HumanApprovalController : ControllerBase
{
    private readonly HumanApprovalQueue      _queue;
    private readonly DirectiveExecutionService _execution;

    public HumanApprovalController(
        HumanApprovalQueue       queue,
        DirectiveExecutionService execution)
    {
        _queue     = queue     ?? throw new ArgumentNullException(nameof(queue));
        _execution = execution ?? throw new ArgumentNullException(nameof(execution));
    }

    // ── GET /v1/approvals/pending ─────────────────────────────────────────────

    /// <summary>
    /// Returns all write-action requests currently awaiting human decision,
    /// ordered oldest-first.
    /// </summary>
    /// <response code="200">List of pending approval requests (may be empty).</response>
    [HttpGet("pending")]
    [ProducesResponseType(typeof(IReadOnlyList<ApprovalResponse>), StatusCodes.Status200OK)]
    public IActionResult GetPending()
    {
        var pending = _queue.GetPendingRequests()
                            .Select(r => r.ToResponse())
                            .ToList();
        return Ok(pending);
    }

    // ── POST /v1/approvals/{id}/approve ──────────────────────────────────────

    /// <summary>
    /// Approve a pending write-action request identified by <paramref name="id"/>.
    /// The action is immediately executed via DirectiveExecutionService.
    /// </summary>
    /// <param name="id">The RequestId of the approval record.</param>
    /// <param name="request">Body containing the operator identity.</param>
    /// <response code="200">Action approved and executed.</response>
    /// <response code="400">ApprovedBy is missing or request body is invalid.</response>
    /// <response code="404">No pending request with the given ID.</response>
    /// <response code="409">Request has already been decided (approved or rejected).</response>
    [HttpPost("{id}/approve")]
    [ProducesResponseType(typeof(ApprovalResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public IActionResult Approve(string id, [FromBody] ApproveActionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.ApprovedBy))
            return BadRequest("ApprovedBy is required and must not be empty.");

        var record = _queue.GetById(id);
        if (record is null)
            return NotFound($"No approval request found with ID '{id}'.");

        if (record.Status != HumanApprovalStatus.Pending)
            return Conflict(
                $"Request '{id}' is already in terminal state {record.Status}. " +
                "It cannot be approved again.");

        // Human-initiated approval.
        var approved = _queue.ApproveRequest(id, request.ApprovedBy);
        if (!approved)
            return Conflict($"Could not approve request '{id}' — it may have been decided concurrently.");

        // Execute the approved action through the safety pipeline.
        var result = _execution.ExecuteApproved(id, request.ApprovedBy);

        return Ok(new
        {
            record.ToResponse().RequestId,
            record.ToResponse().ActionType,
            ApprovedBy      = request.ApprovedBy,
            ApprovedAt      = record.ApprovedAt,
            ExecutionResult = result,
        });
    }

    // ── POST /v1/approvals/{id}/reject ────────────────────────────────────────

    /// <summary>
    /// Reject a pending write-action request identified by <paramref name="id"/>.
    /// The action will never be executed.
    /// </summary>
    /// <param name="id">The RequestId of the approval record.</param>
    /// <param name="request">Optional body with a rejection reason.</param>
    /// <response code="200">Request rejected. Action will not be executed.</response>
    /// <response code="404">No pending request with the given ID.</response>
    /// <response code="409">Request has already been decided.</response>
    [HttpPost("{id}/reject")]
    [ProducesResponseType(typeof(ApprovalResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public IActionResult Reject(string id, [FromBody] RejectActionRequest? request = null)
    {
        var record = _queue.GetById(id);
        if (record is null)
            return NotFound($"No approval request found with ID '{id}'.");

        if (record.Status != HumanApprovalStatus.Pending)
            return Conflict(
                $"Request '{id}' is already in terminal state {record.Status}. " +
                "It cannot be rejected again.");

        var rejected = _queue.RejectRequest(id, request?.Reason);
        if (!rejected)
            return Conflict($"Could not reject request '{id}' — it may have been decided concurrently.");

        return Ok(record.ToResponse());
    }
}
