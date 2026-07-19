// ══════════════════════════════════════════════════════════════════════════════
// META7 Operator API Tests — Human Approval Layer Integration Tests
//
// Verifies deterministic behaviour of the full approval pipeline:
//   1. Write actions → Pending (never auto-executed)
//   2. Approval → action executed exactly once
//   3. Rejection → action never executed
//   4. Domain allowlist enforcement
//   5. SAFE_LOCK blocks all executions
//   6. Controller endpoints return correct HTTP status codes
// ══════════════════════════════════════════════════════════════════════════════

using Moq;
using META7.Operator.Api.Approval;
using META7.Operator.Api.Services;
using META7.Operator.Contracts.Approval;
using Microsoft.AspNetCore.Mvc;

namespace META7.Operator.Api.Tests;

public class OperatorApiApprovalTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (
        HumanApprovalQueue        queue,
        HumanApprovalGateway      gateway,
        Mock<IRevenueActionExecutor> mockExecutor,
        Mock<ISafeLockProvider>   mockSafeLock,
        DirectiveExecutionService execution,
        HumanApprovalController   controller)
        BuildPipeline(
            bool safeLockActive       = false,
            IReadOnlySet<string>? allowedDomains = null)
    {
        var queue       = new HumanApprovalQueue();
        var gateway     = new HumanApprovalGateway(queue, allowedDomains);
        var mockExec    = new Mock<IRevenueActionExecutor>(MockBehavior.Strict);
        var mockLock    = new Mock<ISafeLockProvider>();

        mockLock.SetupGet(s => s.IsSafeLockActive).Returns(safeLockActive);

        var execution   = new DirectiveExecutionService(gateway, queue, mockExec.Object, mockLock.Object);
        var controller  = new HumanApprovalController(queue, execution);

        return (queue, gateway, mockExec, mockLock, execution, controller);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 1. Write actions must be intercepted and queued as Pending — never executed
    // ══════════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(OperatorActionType.SubmitLeadForm)]
    [InlineData(OperatorActionType.RequestCallback)]
    [InlineData(OperatorActionType.TriggerWebhook)]
    [InlineData(OperatorActionType.CreateSupportTicket)]
    [InlineData(OperatorActionType.RegisterInterest)]
    public void WriteAction_IsIntercepted_ReturnsSucceededPending(OperatorActionType actionType)
    {
        var (queue, _, mockExec, _, execution, _) = BuildPipeline();

        var result = execution.Submit("DIR-001", actionType, "{}");

        Assert.True(result.Succeeded, "Submit should succeed (queuing counts as success)");
        Assert.Contains("Pending", result.Reason);

        // Executor must NOT have been called
        mockExec.VerifyNoOtherCalls();

        // Exactly one record in queue, status Pending
        var pending = queue.GetPendingRequests();
        Assert.Single(pending);
        Assert.Equal(HumanApprovalStatus.Pending, pending[0].Status);
        Assert.Equal(actionType, pending[0].ActionType);
    }

    [Fact]
    public void ReadOnlyAction_IsNotIntercepted_ExecutedDirectly()
    {
        var (queue, _, _, _, execution, _) = BuildPipeline();

        var result = execution.Submit("DIR-002", OperatorActionType.QueryStatus, "{}");

        Assert.True(result.Succeeded);
        Assert.Contains("Read-only", result.Reason);
        Assert.Empty(queue.GetPendingRequests()); // nothing queued
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 2. Approval → action executed exactly once
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void WriteAction_AfterApproval_ExecutesActionOnce()
    {
        var (queue, _, mockExec, _, execution, _) = BuildPipeline();

        mockExec
            .Setup(e => e.Execute(It.IsAny<string>(), OperatorActionType.SubmitLeadForm, "{}"))
            .Returns(true);

        // Step 1: submit — should be Pending
        var submitResult = execution.Submit("DIR-003", OperatorActionType.SubmitLeadForm, "{}",
            requestId: "REQ-APPROVE-1");
        Assert.True(submitResult.Succeeded);
        Assert.Equal("REQ-APPROVE-1", submitResult.RequestId);

        // Step 2: human approves
        var approved = queue.ApproveRequest("REQ-APPROVE-1", "operator@meta7.io");
        Assert.True(approved);

        // Step 3: execute approved action
        var execResult = execution.ExecuteApproved("REQ-APPROVE-1", "operator@meta7.io");
        Assert.True(execResult.Succeeded);

        // Executor called exactly once
        mockExec.Verify(
            e => e.Execute("REQ-APPROVE-1", OperatorActionType.SubmitLeadForm, "{}"),
            Times.Once);
    }

    [Fact]
    public void WriteAction_ExecuteApproved_FailsWhenStillPending()
    {
        var (queue, _, mockExec, _, execution, _) = BuildPipeline();

        execution.Submit("DIR-004", OperatorActionType.TriggerWebhook, "{}", requestId: "REQ-STILL-PENDING");

        // Attempt to execute without approval
        var execResult = execution.ExecuteApproved("REQ-STILL-PENDING", "operator@meta7.io");

        Assert.False(execResult.Succeeded);
        Assert.Contains("Pending", execResult.Reason);
        mockExec.VerifyNoOtherCalls();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 3. Rejection → action NEVER executed
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void WriteAction_AfterRejection_NeverExecutes()
    {
        var (queue, _, mockExec, _, execution, _) = BuildPipeline();

        execution.Submit("DIR-005", OperatorActionType.RegisterInterest, "{}",
            requestId: "REQ-REJECT-1");

        // Human rejects
        var rejected = queue.RejectRequest("REQ-REJECT-1", "Not approved by policy");
        Assert.True(rejected);

        // Attempt to execute after rejection
        var execResult = execution.ExecuteApproved("REQ-REJECT-1", "operator@meta7.io");
        Assert.False(execResult.Succeeded);
        Assert.Contains("Rejected", execResult.Reason);

        // Executor is NEVER called
        mockExec.VerifyNoOtherCalls();
    }

    [Fact]
    public void RejectedRecord_IsImmutable_CannotBeApprovedAfterRejection()
    {
        var (queue, _, _, _, execution, _) = BuildPipeline();

        execution.Submit("DIR-006", OperatorActionType.CreateSupportTicket, "{}",
            requestId: "REQ-IMMUTABLE");

        queue.RejectRequest("REQ-IMMUTABLE");

        // Attempt to approve a rejected request — must return false (not throw)
        var canApprove = queue.ApproveRequest("REQ-IMMUTABLE", "operator@meta7.io");
        Assert.False(canApprove);
    }

    [Fact]
    public void ApprovedRecord_IsImmutable_CannotBeRejectedAfterApproval()
    {
        var (queue, _, mockExec, _, execution, _) = BuildPipeline();

        mockExec
            .Setup(e => e.Execute(It.IsAny<string>(), OperatorActionType.RequestCallback, "{}"))
            .Returns(true);

        execution.Submit("DIR-007", OperatorActionType.RequestCallback, "{}",
            requestId: "REQ-IMMUTABLE-2");

        queue.ApproveRequest("REQ-IMMUTABLE-2", "operator@meta7.io");
        execution.ExecuteApproved("REQ-IMMUTABLE-2", "operator@meta7.io");

        // Attempt to reject after approval — must return false
        var canReject = queue.RejectRequest("REQ-IMMUTABLE-2");
        Assert.False(canReject);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 4. Domain allowlist enforcement
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void DomainAllowlist_AllowedDomain_Queued()
    {
        var allowed = new HashSet<string> { "meta7.io", "hopecplus.com" };
        var (queue, _, _, _, execution, _) = BuildPipeline(allowedDomains: allowed);

        var result = execution.Submit("DIR-008", OperatorActionType.SubmitLeadForm, "{}",
            domain: "meta7.io");

        Assert.True(result.Succeeded);
        Assert.Single(queue.GetPendingRequests());
    }

    [Fact]
    public void DomainAllowlist_BlockedDomain_ThrowsUnauthorized()
    {
        var allowed = new HashSet<string> { "meta7.io" };
        var (_, gateway, _, _, _, _) = BuildPipeline(allowedDomains: allowed);

        Assert.Throws<UnauthorizedAccessException>(() =>
            gateway.Intercept("DIR-009", OperatorActionType.SubmitLeadForm, "{}",
                domain: "evil-site.com"));
    }

    [Fact]
    public void DomainAllowlist_NoDomain_Queued()
    {
        // When no domain is supplied, allowlist is not checked.
        var allowed = new HashSet<string> { "meta7.io" };
        var (queue, _, _, _, execution, _) = BuildPipeline(allowedDomains: allowed);

        var result = execution.Submit("DIR-010", OperatorActionType.TriggerWebhook, "{}",
            domain: null);

        Assert.True(result.Succeeded);
        Assert.Single(queue.GetPendingRequests());
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 5. SAFE_LOCK enforcement
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void SafeLock_Active_BlocksAllWriteSubmissions()
    {
        var (queue, _, mockExec, _, execution, _) = BuildPipeline(safeLockActive: true);

        var result = execution.Submit("DIR-011", OperatorActionType.SubmitLeadForm, "{}");

        Assert.False(result.Succeeded);
        Assert.Contains("SAFE_LOCK", result.Reason);
        Assert.Empty(queue.GetPendingRequests());
        mockExec.VerifyNoOtherCalls();
    }

    [Fact]
    public void SafeLock_Active_BlocksExecutionEvenAfterApproval()
    {
        // Start with SAFE_LOCK off so we can queue the request.
        var (queue, gateway, mockExec, mockLock, execution, _) = BuildPipeline(safeLockActive: false);

        execution.Submit("DIR-012", OperatorActionType.RegisterInterest, "{}",
            requestId: "REQ-LOCK-EXEC");

        queue.ApproveRequest("REQ-LOCK-EXEC", "operator@meta7.io");

        // Engage SAFE_LOCK before execution.
        mockLock.SetupGet(s => s.IsSafeLockActive).Returns(true);

        var execResult = execution.ExecuteApproved("REQ-LOCK-EXEC", "operator@meta7.io");

        Assert.False(execResult.Succeeded);
        Assert.Contains("SAFE_LOCK", execResult.Reason);
        mockExec.VerifyNoOtherCalls();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 6. Deterministic ordering and queue behaviour
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Queue_GetPending_ReturnedOldestFirst()
    {
        var (queue, _, _, _, _, _) = BuildPipeline();
        var baseTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var r1 = new HumanApprovalRecord("REQ-A", "DIR-A", OperatorActionType.SubmitLeadForm, "{}", baseTime.AddMinutes(2));
        var r2 = new HumanApprovalRecord("REQ-B", "DIR-B", OperatorActionType.TriggerWebhook,  "{}", baseTime.AddMinutes(1));
        var r3 = new HumanApprovalRecord("REQ-C", "DIR-C", OperatorActionType.RequestCallback, "{}", baseTime.AddMinutes(3));

        queue.AddRequest(r1);
        queue.AddRequest(r2);
        queue.AddRequest(r3);

        var pending = queue.GetPendingRequests();

        Assert.Equal(3, pending.Count);
        Assert.Equal("REQ-B", pending[0].RequestId); // oldest
        Assert.Equal("REQ-A", pending[1].RequestId);
        Assert.Equal("REQ-C", pending[2].RequestId); // newest
    }

    [Fact]
    public void Queue_AddDuplicateRequestId_Throws()
    {
        var (queue, _, _, _, _, _) = BuildPipeline();
        var rec = new HumanApprovalRecord("REQ-DUP", "DIR-DUP", OperatorActionType.SubmitLeadForm, "{}", DateTime.UtcNow);

        queue.AddRequest(rec);

        Assert.Throws<ArgumentException>(() => queue.AddRequest(rec));
    }

    [Fact]
    public void Gateway_IsWriteAction_CorrectClassification()
    {
        var (_, gateway, _, _, _, _) = BuildPipeline();

        // Write actions
        Assert.True(gateway.IsWriteAction(OperatorActionType.SubmitLeadForm));
        Assert.True(gateway.IsWriteAction(OperatorActionType.RequestCallback));
        Assert.True(gateway.IsWriteAction(OperatorActionType.TriggerWebhook));
        Assert.True(gateway.IsWriteAction(OperatorActionType.CreateSupportTicket));
        Assert.True(gateway.IsWriteAction(OperatorActionType.RegisterInterest));

        // Read-only actions
        Assert.False(gateway.IsWriteAction(OperatorActionType.QueryStatus));
        Assert.False(gateway.IsWriteAction(OperatorActionType.GetMetrics));
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 7. Controller endpoint behaviour
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Controller_GetPending_ReturnsEmptyList_WhenNoPendingRequests()
    {
        var (_, _, _, _, _, controller) = BuildPipeline();

        var result = controller.GetPending() as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
        var list = Assert.IsAssignableFrom<IReadOnlyList<ApprovalResponse>>(result.Value);
        Assert.Empty(list);
    }

    [Fact]
    public void Controller_Approve_Returns200_WhenRequestPending()
    {
        var (queue, _, mockExec, _, execution, controller) = BuildPipeline();

        mockExec
            .Setup(e => e.Execute(It.IsAny<string>(), OperatorActionType.CreateSupportTicket, "{}"))
            .Returns(true);

        execution.Submit("DIR-013", OperatorActionType.CreateSupportTicket, "{}",
            requestId: "REQ-CTL-APPROVE");

        var result = controller.Approve(
            "REQ-CTL-APPROVE",
            new ApproveActionRequest("operator@meta7.io")) as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);

        mockExec.Verify(
            e => e.Execute("REQ-CTL-APPROVE", OperatorActionType.CreateSupportTicket, "{}"),
            Times.Once);
    }

    [Fact]
    public void Controller_Approve_Returns400_WhenApprovedByMissing()
    {
        var (_, _, _, _, execution, controller) = BuildPipeline();
        execution.Submit("DIR-014", OperatorActionType.SubmitLeadForm, "{}",
            requestId: "REQ-NO-BY");

        var result = controller.Approve("REQ-NO-BY", new ApproveActionRequest(""));
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public void Controller_Approve_Returns404_WhenRequestNotFound()
    {
        var (_, _, _, _, _, controller) = BuildPipeline();

        var result = controller.Approve("NONEXISTENT", new ApproveActionRequest("operator@meta7.io"));
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public void Controller_Reject_Returns200_WhenRequestPending()
    {
        var (_, _, _, _, execution, controller) = BuildPipeline();

        execution.Submit("DIR-015", OperatorActionType.RegisterInterest, "{}",
            requestId: "REQ-CTL-REJECT");

        var result = controller.Reject("REQ-CTL-REJECT",
            new RejectActionRequest("Rejected by policy review")) as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
    }

    [Fact]
    public void Controller_Reject_Returns404_WhenRequestNotFound()
    {
        var (_, _, _, _, _, controller) = BuildPipeline();

        var result = controller.Reject("NONEXISTENT");
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public void Controller_Reject_Returns409_WhenAlreadyDecided()
    {
        var (queue, _, mockExec, _, execution, controller) = BuildPipeline();

        mockExec
            .Setup(e => e.Execute(It.IsAny<string>(), OperatorActionType.TriggerWebhook, "{}"))
            .Returns(true);

        execution.Submit("DIR-016", OperatorActionType.TriggerWebhook, "{}",
            requestId: "REQ-DOUBLE-DECIDE");

        queue.ApproveRequest("REQ-DOUBLE-DECIDE", "operator@meta7.io");

        var result = controller.Reject("REQ-DOUBLE-DECIDE");
        Assert.IsType<ConflictObjectResult>(result);
    }
}
