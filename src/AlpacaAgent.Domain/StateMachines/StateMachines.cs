using AlpacaAgent.Domain.Contracts;

namespace AlpacaAgent.Domain.StateMachines;

public enum PortfolioEligibilityEvent { ApprovalAdded, ApprovalRemoved, BrokerSnapshotReconciled, SharesReserved, SharesReleased, ObligationObserved, InconsistencyDetected, InconsistencyResolved, PauseActivated, PauseCleared }
public sealed record TransitionResult<TState>(TState State, bool Accepted, bool Changed, bool SafeHalt, string Reason);
public sealed record PortfolioEligibilityContext(bool Approved, decimal VerifiedShares, decimal ReservedShares, bool Reconciled, bool Paused);

public static class PortfolioEligibilityStateMachine
{
    public static PortfolioEligibilityState Derive(PortfolioEligibilityContext value)
    {
        if (value.Paused) return PortfolioEligibilityState.Paused;
        if (!value.Reconciled) return PortfolioEligibilityState.InconsistentOrUnreconciled;
        if (!value.Approved) return PortfolioEligibilityState.NoApprovedHolding;
        if (value.VerifiedShares < 100m) return PortfolioEligibilityState.ApprovedButInsufficientShares;
        var free = value.VerifiedShares - value.ReservedShares;
        if (free < 100m) return PortfolioEligibilityState.NoFreeCoverage;
        return value.ReservedShares > 0m ? PortfolioEligibilityState.SharesPartiallyReserved : PortfolioEligibilityState.EligibleSharesAvailable;
    }
    public static TransitionResult<PortfolioEligibilityState> Apply(PortfolioEligibilityState current, PortfolioEligibilityEvent @event, PortfolioEligibilityContext next)
    {
        var derived = Derive(next);
        var valid = @event switch
        {
            PortfolioEligibilityEvent.PauseActivated => next.Paused,
            PortfolioEligibilityEvent.PauseCleared => !next.Paused,
            PortfolioEligibilityEvent.ApprovalAdded => next.Approved,
            PortfolioEligibilityEvent.ApprovalRemoved => !next.Approved,
            PortfolioEligibilityEvent.InconsistencyDetected => !next.Reconciled,
            PortfolioEligibilityEvent.InconsistencyResolved or PortfolioEligibilityEvent.BrokerSnapshotReconciled => next.Reconciled,
            PortfolioEligibilityEvent.SharesReserved => next.ReservedShares > 0,
            PortfolioEligibilityEvent.SharesReleased => next.ReservedShares == 0,
            PortfolioEligibilityEvent.ObligationObserved => next.ReservedShares >= 100,
            _ => true
        };
        return valid ? new(derived, true, derived != current, false, derived == current ? "duplicate-event" : "accepted") : new(current, false, false, true, "INVALID_TRANSITION");
    }
}

public sealed record OrderNormalization(OrderState State, string Reason, bool RequiresPoll = false);
public static class OrderStateMachine
{
    public static OrderNormalization Normalize(string rawStatus) => rawStatus.ToLowerInvariant() switch
    {
        "accepted" or "pending_new" or "new" or "held" or "accepted_for_bidding" => new(OrderState.Working, "working"),
        "partially_filled" => new(OrderState.PartiallyFilled, "partial-fill"),
        "pending_cancel" => new(OrderState.CancelPending, "cancel-pending"),
        "filled" => new(OrderState.Filled, "filled"),
        "canceled" => new(OrderState.Canceled, "canceled"),
        "rejected" => new(OrderState.Rejected, "rejected"),
        "expired" => new(OrderState.Expired, "expired"),
        "pending_replace" => new(OrderState.ReconciliationRequired, "externally-initiated-replace", true),
        "replaced" => new(OrderState.ReplacedExternal, "reconcile-predecessor-successor", true),
        "done_for_day" or "stopped" or "suspended" or "calculated" => new(OrderState.ReconciliationRequired, "uncommon-status", true),
        _ => new(OrderState.ReconciliationRequired, "unknown-status", true)
    };
    public static OrderProjection Apply(OrderProjection current, OrderObservation observation)
    {
        if (current.ObservationIds.Contains(observation.Id)) return current;
        var ids = current.ObservationIds.Append(observation.Id).ToHashSet(StringComparer.Ordinal);
        var quantity = observation.CorrectedCumulativeFill ?? current.CumulativeFill;
        if (observation.ObservedAt < current.LastObservedAt && IsTerminal(current.State)) return current with { ObservationIds = ids };
        return new(Normalize(observation.RawStatus).State, quantity, observation.ObservedAt, ids);
    }
    public static bool IsTerminal(OrderState state) => state is OrderState.Filled or OrderState.Canceled or OrderState.Rejected or OrderState.Expired or OrderState.ReplacedExternal;
}
public sealed record OrderObservation(string Id, string RawStatus, DateTimeOffset ObservedAt, decimal? CorrectedCumulativeFill = null);
public sealed record OrderProjection(OrderState State, decimal CumulativeFill, DateTimeOffset LastObservedAt, IReadOnlySet<string> ObservationIds);

public enum OptionLifecycleEvent { EntrySubmitted, EntryFilled, CloseReviewDue, CloseSubmitted, NearExpirationReached, AssignmentRiskObserved, ExpirationQuoteObserved, BrokerExpirationConfirmed, AssignmentObserved, CloseFillConfirmed, InconsistencyDetected }
public sealed record LifecycleProjection(OptionLifecycleState State, int OpenContracts, IReadOnlySet<string> EventIds);
public static class OptionLifecycleStateMachine
{
    public static TransitionResult<LifecycleProjection> Apply(LifecycleProjection current, string eventId, OptionLifecycleEvent @event, int confirmedQuantity = 0)
    {
        if (current.EventIds.Contains(eventId)) return new(current, true, false, false, "duplicate-event");
        var state = @event switch
        {
            OptionLifecycleEvent.EntrySubmitted when current.State == OptionLifecycleState.NoPosition => OptionLifecycleState.EntryOrderOpen,
            OptionLifecycleEvent.EntryFilled when current.State == OptionLifecycleState.EntryOrderOpen => OptionLifecycleState.ShortCallOpen,
            OptionLifecycleEvent.CloseReviewDue when current.State is OptionLifecycleState.ShortCallOpen or OptionLifecycleState.NearExpiration => OptionLifecycleState.CloseReview,
            OptionLifecycleEvent.CloseSubmitted when current.State == OptionLifecycleState.CloseReview => OptionLifecycleState.CloseOrderOpen,
            OptionLifecycleEvent.NearExpirationReached when current.State == OptionLifecycleState.ShortCallOpen => OptionLifecycleState.NearExpiration,
            OptionLifecycleEvent.AssignmentRiskObserved when current.State is OptionLifecycleState.ShortCallOpen or OptionLifecycleState.NearExpiration => OptionLifecycleState.AssignmentPossible,
            OptionLifecycleEvent.ExpirationQuoteObserved when current.State is not OptionLifecycleState.NoPosition and not OptionLifecycleState.Closed => OptionLifecycleState.ExpiredPendingConfirmation,
            OptionLifecycleEvent.BrokerExpirationConfirmed when current.State == OptionLifecycleState.ExpiredPendingConfirmation => OptionLifecycleState.Closed,
            OptionLifecycleEvent.AssignmentObserved when current.State is not OptionLifecycleState.NoPosition and not OptionLifecycleState.Closed => OptionLifecycleState.AssignedPendingReconciliation,
            OptionLifecycleEvent.CloseFillConfirmed when current.State == OptionLifecycleState.CloseOrderOpen => confirmedQuantity >= current.OpenContracts ? OptionLifecycleState.Closed : OptionLifecycleState.ShortCallOpen,
            OptionLifecycleEvent.InconsistencyDetected => OptionLifecycleState.ExceptionOrSafeHalt,
            _ => (OptionLifecycleState?)null
        };
        if (state is null) return new(current, false, false, true, "INVALID_TRANSITION");
        var remaining = @event == OptionLifecycleEvent.CloseFillConfirmed ? Math.Max(0, current.OpenContracts - confirmedQuantity) : current.OpenContracts;
        var next = new LifecycleProjection(state.Value, remaining, current.EventIds.Append(eventId).ToHashSet(StringComparer.Ordinal));
        return new(next, true, true, state == OptionLifecycleState.ExceptionOrSafeHalt, "accepted");
    }
}

public sealed record RollRejection(string Code, IReadOnlyList<string> CreatedArtifacts);
public static class MvpLifecyclePolicy { public static RollRejection RejectRoll() => new("ROLL_OUT_OF_SCOPE", []); }
