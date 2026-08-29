using AlpacaAgent.Domain.Contracts;
using AlpacaAgent.Domain.StateMachines;
using AlpacaAgent.Domain.Workflow;
using Xunit;

namespace AlpacaAgent.UnitTests.Acceptance;

public sealed class GeneratedStateMachineAcceptanceTests
{
    [Fact, Trait("StableId", "UT-020"), Trait("GateStatus", "ActiveThroughPhase3")]
    public void PortfolioEligibility_TransitionMatrix_AllDeclaredTransitionsMatchSpecification()
    {
        Assert.Equal(PortfolioEligibilityState.NoApprovedHolding, PortfolioEligibilityStateMachine.Derive(new(false, 100, 0, true, false)));
        Assert.Equal(PortfolioEligibilityState.ApprovedButInsufficientShares, PortfolioEligibilityStateMachine.Derive(new(true, 99, 0, true, false)));
        Assert.Equal(PortfolioEligibilityState.EligibleSharesAvailable, PortfolioEligibilityStateMachine.Derive(new(true, 100, 0, true, false)));
        Assert.Equal(PortfolioEligibilityState.SharesPartiallyReserved, PortfolioEligibilityStateMachine.Derive(new(true, 200, 100, true, false)));
        Assert.Equal(PortfolioEligibilityState.NoFreeCoverage, PortfolioEligibilityStateMachine.Derive(new(true, 100, 100, true, false)));
        Assert.Equal(PortfolioEligibilityState.InconsistentOrUnreconciled, PortfolioEligibilityStateMachine.Derive(new(true, 100, 0, false, false)));
        Assert.Equal(PortfolioEligibilityState.Paused, PortfolioEligibilityStateMachine.Derive(new(true, 100, 0, true, true)));
    }
    [Fact, Trait("StableId", "UT-021"), Trait("GateStatus", "ActiveThroughPhase3")]
    public void PortfolioEligibility_InvalidTransition_EntersSafeHaltWithoutMutation()
    {
        var result = PortfolioEligibilityStateMachine.Apply(PortfolioEligibilityState.NoApprovedHolding, PortfolioEligibilityEvent.ApprovalAdded, new(false, 0, 0, true, false));
        Assert.False(result.Accepted); Assert.True(result.SafeHalt); Assert.Equal(PortfolioEligibilityState.NoApprovedHolding, result.State);
    }
    [Fact, Trait("StableId", "UT-022"), Trait("GateStatus", "ActiveThroughPhase3")]
    public void OrderState_TransitionMatrix_AllBrokerStatusesNormalizeDeterministically()
    {
        var expected = new Dictionary<string, OrderState> { ["accepted"] = OrderState.Working, ["pending_new"] = OrderState.Working, ["new"] = OrderState.Working, ["held"] = OrderState.Working, ["accepted_for_bidding"] = OrderState.Working, ["partially_filled"] = OrderState.PartiallyFilled, ["done_for_day"] = OrderState.ReconciliationRequired, ["pending_cancel"] = OrderState.CancelPending, ["pending_replace"] = OrderState.ReconciliationRequired, ["stopped"] = OrderState.ReconciliationRequired, ["suspended"] = OrderState.ReconciliationRequired, ["calculated"] = OrderState.ReconciliationRequired, ["filled"] = OrderState.Filled, ["canceled"] = OrderState.Canceled, ["expired"] = OrderState.Expired, ["rejected"] = OrderState.Rejected, ["replaced"] = OrderState.ReplacedExternal };
        Assert.All(expected, pair => Assert.Equal(pair.Value, OrderStateMachine.Normalize(pair.Key).State));
    }
    [Fact, Trait("StableId", "UT-023"), Trait("GateStatus", "ActiveThroughPhase3")]
    public void OrderState_UnknownBrokerStatus_RequiresReconciliation() { var value = OrderStateMachine.Normalize("future"); Assert.Equal(OrderState.ReconciliationRequired, value.State); Assert.True(value.RequiresPoll); }
    [Fact, Trait("StableId", "UT-024"), Trait("GateStatus", "ActiveThroughPhase3")]
    public void OrderState_TerminalObservationFollowedByOlderEvent_DoesNotRegress()
    {
        var now = DateTimeOffset.UtcNow; var current = new OrderProjection(OrderState.Filled, 1, now, new HashSet<string>());
        Assert.Equal(OrderState.Filled, OrderStateMachine.Apply(current, new("older", "new", now.AddSeconds(-1))).State);
    }
    [Fact, Trait("StableId", "UT-025"), Trait("GateStatus", "ActiveThroughPhase3")]
    public void OrderState_TradeCorrectionOrBust_RecomputesCumulativeFill()
    {
        var now = DateTimeOffset.UtcNow; var current = new OrderProjection(OrderState.PartiallyFilled, 2, now, new HashSet<string>());
        Assert.Equal(1, OrderStateMachine.Apply(current, new("correction", "partially_filled", now.AddSeconds(1), 1)).CumulativeFill);
    }
    [Fact, Trait("StableId", "UT-026"), Trait("GateStatus", "ActiveThroughPhase3")]
    public void OptionLifecycle_TransitionMatrix_AllDeclaredTransitionsMatchSpecification()
    {
        var state = new LifecycleProjection(OptionLifecycleState.NoPosition, 1, new HashSet<string>());
        state = OptionLifecycleStateMachine.Apply(state, "1", OptionLifecycleEvent.EntrySubmitted).State;
        state = OptionLifecycleStateMachine.Apply(state, "2", OptionLifecycleEvent.EntryFilled).State;
        Assert.Equal(OptionLifecycleState.ShortCallOpen, state.State);
    }
    [Fact, Trait("StableId", "UT-027"), Trait("GateStatus", "ActiveThroughPhase3")]
    public void OptionLifecycle_AssignmentObservedAtAnyAge_EntersAssignedPendingReconciliation() => Assert.Equal(OptionLifecycleState.AssignedPendingReconciliation, OptionLifecycleStateMachine.Apply(Open(), "a", OptionLifecycleEvent.AssignmentObserved).State.State);
    [Fact, Trait("StableId", "UT-028"), Trait("GateStatus", "ActiveThroughPhase3")]
    public void OptionLifecycle_ExpiryQuoteSeenWithoutBrokerConfirmation_RemainsPending() => Assert.Equal(OptionLifecycleState.ExpiredPendingConfirmation, OptionLifecycleStateMachine.Apply(Open(), "e", OptionLifecycleEvent.ExpirationQuoteObserved).State.State);
    [Fact, Trait("StableId", "UT-029"), Trait("GateStatus", "ActiveThroughPhase3")]
    public void OptionLifecycle_CloseFillConfirmed_ReleasesOnlyClosedQuantity()
    {
        var result = OptionLifecycleStateMachine.Apply(new(OptionLifecycleState.CloseOrderOpen, 3, new HashSet<string>()), "c", OptionLifecycleEvent.CloseFillConfirmed, 1).State;
        Assert.Equal(2, result.OpenContracts); Assert.Equal(OptionLifecycleState.ShortCallOpen, result.State);
    }
    [Fact, Trait("StableId", "UT-030"), Trait("GateStatus", "ActiveThroughPhase3")]
    public void TerminalWorkflowCycle_AnyFurtherAdvance_IsRejected() => Assert.False(WorkflowCycleStateMachine.MayAdvance(WorkflowTerminalOutcome.Completed));
    [Fact, Trait("StableId", "UT-031"), Trait("GateStatus", "ActiveThroughPhase3")]
    public void Lifecycle_RollRequestedInMvp_IsRejectedAsOutOfScope() { var result = MvpLifecyclePolicy.RejectRoll(); Assert.Equal("ROLL_OUT_OF_SCOPE", result.Code); Assert.Empty(result.CreatedArtifacts); }
    [Fact, Trait("StableId", "UT-032"), Trait("GateStatus", "ActiveThroughPhase3")]
    public void StateTransition_DuplicateEvent_IsIdempotent()
    {
        var once = OptionLifecycleStateMachine.Apply(Open(), "same", OptionLifecycleEvent.NearExpirationReached).State;
        var twice = OptionLifecycleStateMachine.Apply(once, "same", OptionLifecycleEvent.NearExpirationReached);
        Assert.False(twice.Changed); Assert.Equal(once, twice.State);
    }
    private static LifecycleProjection Open() => new(OptionLifecycleState.ShortCallOpen, 1, new HashSet<string>());
}
