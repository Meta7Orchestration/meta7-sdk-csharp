using META7.CaptainM7A.Safety.SafeLock.Domain;
using META7.CaptainM7A.Safety.SafeLock.Services;

namespace META7.CaptainM7A.Tests;

public class SafeLockControllerTests
{
    [Fact]
    public void InitialState_IsUnlocked()
    {
        var controller = CreateController();

        Assert.False(controller.IsActive);
        Assert.Equal(0, controller.Version);
        Assert.Equal(SafeLockState.Unlocked, controller.Snapshot.State);
        Assert.Equal(SafeLockReason.Unspecified, controller.Snapshot.Reason);
    }

    [Fact]
    public void Activation_LocksAndAdvancesVersion()
    {
        var controller = CreateController();

        var activated = controller.Activate(SafeLockReason.Sovereign, "SOVEREIGN", "Manual override", "Operator request");

        Assert.True(controller.IsActive);
        Assert.Equal(1, controller.Version);
        Assert.Equal(SafeLockState.Active, activated.Snapshot.State);
        Assert.Equal("SOVEREIGN", activated.Snapshot.ActorId);
        Assert.Equal("Manual override", activated.Snapshot.AuditMessage);
        Assert.Equal("Operator request", activated.Snapshot.ReasonDetail);
    }

    [Fact]
    public void RepeatedActivation_RemainsLockedAndAdvancesVersion()
    {
        var controller = CreateController();

        controller.Activate(SafeLockReason.Store, "STORE", "First activation", "first");
        var activated = controller.Activate(SafeLockReason.Store, "STORE", "Second activation", "second");

        Assert.True(controller.IsActive);
        Assert.Equal(2, controller.Version);
        Assert.Equal("Second activation", activated.Snapshot.AuditMessage);
        Assert.Equal("second", activated.Snapshot.ReasonDetail);
    }

    [Fact]
    public void LockedSafeLock_BlocksCommandAdmission()
    {
        var controller = CreateController();
        var gateway = new CommandGateway(
            controller,
            () => controller.Activate(SafeLockReason.Compatibility, "GATEWAY", "SAFE_LOCK active"),
            () => controller.Release(controller.Version, SafeLockReason.Compatibility, "GATEWAY", "SAFE_LOCK released"));
        var router = new DistributedCommandRouter(controller);
        router.RegisterShard(new AgentShard(AgentRole.COMMANDER, 0));

        gateway.ActivateSafeLock();

        var directive = new Directive("D-001", DirectiveType.ANALYZE, "payload", RiskLevel.LOW, DateTime.UtcNow);
        var gatewayResult = gateway.Evaluate(directive);
        var routeResult = router.Route(directive);

        Assert.Equal(GatewayVerdict.BLOCKED_SAFELOCK, gatewayResult.Verdict);
        Assert.Equal("SAFE_LOCK active", gatewayResult.Reason);
        Assert.False(routeResult.Delivered);
        Assert.Equal("SAFE_LOCK active", routeResult.Message);
    }

    [Fact]
    public void Release_WithCorrectExpectedVersion_Unlocks()
    {
        var controller = CreateController();
        controller.Activate(SafeLockReason.Manual, "USER", "activate", "activate");

        var released = controller.Release(1, SafeLockReason.Manual, "USER", "release", "release");

        Assert.False(controller.IsActive);
        Assert.Equal(2, controller.Version);
        Assert.Equal(SafeLockState.Unlocked, released.Snapshot.State);
    }

    [Fact]
    public void Release_WithStaleVersion_IsRejected()
    {
        var controller = CreateController();
        controller.Activate(SafeLockReason.Manual, "USER", "activate", "activate");
        controller.Activate(SafeLockReason.Manual, "USER", "activate again", "activate again");

        var exception = Assert.Throws<SafeLockException>(() =>
            controller.Release(1, SafeLockReason.Manual, "USER", "release", "release"));

        Assert.Equal(1, exception.ExpectedVersion);
        Assert.Equal(2, exception.ActualVersion);
    }

    [Fact]
    public void Activation_PreservesReasonAndAuditMetadata()
    {
        var controller = CreateController();

        var activated = controller.Activate(
            SafeLockReason.Sovereign,
            "SOVEREIGN",
            "Manual release denied",
            "Out-of-band audit trail");

        Assert.Equal(SafeLockReason.Sovereign, activated.Snapshot.Reason);
        Assert.Equal("SOVEREIGN", activated.Snapshot.ActorId);
        Assert.Equal("Manual release denied", activated.Snapshot.AuditMessage);
        Assert.Equal("Out-of-band audit trail", activated.Snapshot.ReasonDetail);
        Assert.Equal(new DateTime(2026, 7, 19, 15, 28, 1, DateTimeKind.Utc), activated.Snapshot.ChangedAt);
    }

    private static SafeLockController CreateController()
    {
        var timestamps = new Queue<DateTime>(new[]
        {
            new DateTime(2026, 7, 19, 15, 28, 0, DateTimeKind.Utc),
            new DateTime(2026, 7, 19, 15, 28, 1, DateTimeKind.Utc),
            new DateTime(2026, 7, 19, 15, 28, 2, DateTimeKind.Utc),
            new DateTime(2026, 7, 19, 15, 28, 3, DateTimeKind.Utc),
            new DateTime(2026, 7, 19, 15, 28, 4, DateTimeKind.Utc),
            new DateTime(2026, 7, 19, 15, 28, 5, DateTimeKind.Utc),
            new DateTime(2026, 7, 19, 15, 28, 6, DateTimeKind.Utc),
            new DateTime(2026, 7, 19, 15, 28, 7, DateTimeKind.Utc),
        });

        return new SafeLockController(() => timestamps.Dequeue());
    }
}
