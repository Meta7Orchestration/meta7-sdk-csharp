using META7.CaptainM7A;

namespace META7.CaptainM7A.Tests;

public class SmokeTests
{
    [Fact]
    public void CommandGateway_BlocksCriticalRiskAndTracksBudget()
    {
        var gateway = new CommandGateway();
        var directive = new Directive("D-001", DirectiveType.ANALYZE, "payload", RiskLevel.LOW, DateTime.UtcNow);

        var approved = gateway.Evaluate(directive, costEstimate: 25.0);
        var blocked = gateway.Evaluate(
            new Directive("D-002", DirectiveType.EXECUTE, "payload", RiskLevel.CRITICAL, DateTime.UtcNow));

        Assert.Equal(GatewayVerdict.APPROVED, approved.Verdict);
        Assert.Same(directive, approved.PassedDirective);
        Assert.Equal(25.0, gateway.BudgetUsed);
        Assert.Equal(GatewayVerdict.BLOCKED_RISK, blocked.Verdict);
        Assert.Null(blocked.PassedDirective);
    }

    [Fact]
    public void CanonicalEventStore_MaintainsHashChainAndEnforcesSafeLock()
    {
        var store = new CanonicalEventStore();

        store.Append(EventType.InvariantChecked, "ACTOR-1", "payload-1");
        store.Append(EventType.BarrierReleased, "ACTOR-2", "payload-2");

        Assert.True(store.VerifyChain());

        store.ActivateSafeLock();

        var exception = Assert.Throws<InvalidOperationException>(
            () => store.Append(EventType.LaunchExecuted, "ACTOR-3", "blocked"));
        Assert.Contains("SAFE_LOCK blocks LaunchExecuted", exception.Message);
    }

    [Fact]
    public void DistributedCommandRouter_UsesDeterministicHashRoutingForSameDirectiveId()
    {
        var router = new DistributedCommandRouter();
        router.RegisterShard(new AgentShard(AgentRole.COMMANDER, 0));
        router.RegisterShard(new AgentShard(AgentRole.SENTINEL, 1));
        router.RegisterShard(new AgentShard(AgentRole.FORGE, 2));

        var first = router.Route(
            new Directive("ROUTE-42", DirectiveType.ANALYZE, "payload-a", RiskLevel.MEDIUM, DateTime.UtcNow),
            RoutingStrategy.HashMod);
        var second = router.Route(
            new Directive("ROUTE-42", DirectiveType.REPORT, "payload-b", RiskLevel.LOW, DateTime.UtcNow),
            RoutingStrategy.HashMod);

        Assert.True(first.Delivered);
        Assert.True(second.Delivered);
        Assert.Equal(first.Decision.ShardIndex, second.Decision.ShardIndex);
    }

    [Fact]
    public void BarrierNetwork_LocksAfterRequiredCheckInsAndResetsOnRelease()
    {
        var barrier = new BarrierNetwork(new[] { AgentRole.COMMANDER, AgentRole.FORGE });

        Assert.True(barrier.CheckIn(AgentRole.COMMANDER));
        Assert.Equal(BarrierState.OPEN, barrier.State);

        Assert.True(barrier.CheckIn(AgentRole.FORGE));
        Assert.Equal(BarrierState.LOCKED, barrier.State);
        Assert.True(barrier.AllCheckedIn);

        Assert.True(barrier.Release());
        Assert.Equal(BarrierState.OPEN, barrier.State);
        Assert.Equal(2, barrier.Epoch);
        Assert.Equal(0, barrier.CheckedInCount);
    }
}
