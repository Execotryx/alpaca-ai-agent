using AlpacaAgent.Application.Acceptance;
using Xunit;

namespace AlpacaAgent.UnitTests.Acceptance;

public sealed class GeneratedExecutionAcceptanceTests : AcceptanceTestBase
{
    private readonly ExecutionFutureBoundary boundary = new();

    [Fact]
    [Trait("StableId", "UT-170")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void IntentFactory_ApprovedAction_CreatesImmutableExactOrderFields() => AssertExpectedRed("UT-170", @"Only approved fields enter intent.", boundary.IntentFactory_ApprovedAction_CreatesImmutableExactOrderFields());

    [Fact]
    [Trait("StableId", "UT-171")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void IntentFactory_SameLogicalAction_CreatesSameStableIntentAndClientOrderId() => AssertExpectedRed("UT-171", @"Retry/restart identity is deterministic and within the 128-character limit.", boundary.IntentFactory_SameLogicalAction_CreatesSameStableIntentAndClientOrderId());

    [Fact]
    [Trait("StableId", "UT-172")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void IntentFactory_ChangedExecutableField_ChangesIdentityAndRequiresNewApproval() => AssertExpectedRed("UT-172", @"An old identity cannot disguise a changed order.", boundary.IntentFactory_ChangedExecutableField_ChangesIdentityAndRequiresNewApproval());

    [Fact]
    [Trait("StableId", "UT-173")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void ExecuteHandler_NoCommittedCheckpointOrReadyIntent_DoesNotCallBroker() => AssertExpectedRed("UT-173", @"Fake broker observes zero writes.", boundary.ExecuteHandler_NoCommittedCheckpointOrReadyIntent_DoesNotCallBroker());

    [Fact]
    [Trait("StableId", "UT-174")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void ExecuteHandler_ReadyIntent_CallsBrokerOnceWithStoredClientOrderId() => AssertExpectedRed("UT-174", @"No generated-at-call identity is permitted.", boundary.ExecuteHandler_ReadyIntent_CallsBrokerOnceWithStoredClientOrderId());

    [Fact]
    [Trait("StableId", "UT-175")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void ExecuteHandler_DefinitiveAcceptance_RecordsAcceptedNotFilled() => AssertExpectedRed("UT-175", @"Acceptance does not create a position.", boundary.ExecuteHandler_DefinitiveAcceptance_RecordsAcceptedNotFilled());

    [Fact]
    [Trait("StableId", "UT-176")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void ExecuteHandler_DefinitiveRejection_RecordsReasonAndNoRetry() => AssertExpectedRed("UT-176", @"Terminal rejection is not blindly retried.", boundary.ExecuteHandler_DefinitiveRejection_RecordsReasonAndNoRetry());

    [Fact]
    [Trait("StableId", "UT-177")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void ExecuteHandler_TimeoutDisconnectOrAmbiguousBody_EntersSubmittedUnknown() => AssertExpectedRed("UT-177", @"Ambiguous POST outcomes all choose reconciliation.", boundary.ExecuteHandler_TimeoutDisconnectOrAmbiguousBody_EntersSubmittedUnknown());

    [Fact]
    [Trait("StableId", "UT-178")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void ExecuteHandler_RateLimitBeforeKnownSubmission_FollowsFrozenAmbiguityRule() => AssertExpectedRed("UT-178", @"429 handling is explicit and cannot assume absence without proof.", boundary.ExecuteHandler_RateLimitBeforeKnownSubmission_FollowsFrozenAmbiguityRule());

    [Fact]
    [Trait("StableId", "UT-179")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void Reconciler_SubmittedUnknownLookupFindsOrder_AdoptsBrokerIdentityWithoutResubmit() => AssertExpectedRed("UT-179", @"Accepted/open result prevents another POST.", boundary.Reconciler_SubmittedUnknownLookupFindsOrder_AdoptsBrokerIdentityWithoutResubmit());

    [Fact]
    [Trait("StableId", "UT-180")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void Reconciler_SubmittedUnknownLookupFindsFilledRejectedOrCanceled_MapsTerminalTruth() => AssertExpectedRed("UT-180", @"Theory maps each result and writes no duplicate.", boundary.Reconciler_SubmittedUnknownLookupFindsFilledRejectedOrCanceled_MapsTerminalTruth());

    [Fact]
    [Trait("StableId", "UT-181")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void Reconciler_SubmittedUnknownLookupAbsentButApprovalInvalid_SafeHalts() => AssertExpectedRed("UT-181", @"Absence alone is insufficient for retry.", boundary.Reconciler_SubmittedUnknownLookupAbsentButApprovalInvalid_SafeHalts());

    [Fact]
    [Trait("StableId", "UT-182")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void Reconciler_SubmittedUnknownStillUnresolved_SchedulesBoundedPollAndNoWrite() => AssertExpectedRed("UT-182", @"Unknown remains explicit.", boundary.Reconciler_SubmittedUnknownStillUnresolved_SchedulesBoundedPollAndNoWrite());

    [Fact]
    [Trait("StableId", "UT-183")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void OrderObservation_DuplicateEvent_IsIdempotent() => AssertExpectedRed("UT-183", @"No double fill or double reservation release.", boundary.OrderObservation_DuplicateEvent_IsIdempotent());

    [Fact]
    [Trait("StableId", "UT-184")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void OrderObservation_OutOfOrderPartialAndFill_UsesMonotonicCumulativeTruth() => AssertExpectedRed("UT-184", @"Event order cannot reduce filled quantity.", boundary.OrderObservation_OutOfOrderPartialAndFill_UsesMonotonicCumulativeTruth());

    [Fact]
    [Trait("StableId", "UT-185")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void PartialFill_RemainderRetainsCoverageAndOnlyRemainderMayBeCanceled() => AssertExpectedRed("UT-185", @"Filled and working quantities are tracked independently.", boundary.PartialFill_RemainderRetainsCoverageAndOnlyRemainderMayBeCanceled());

    [Fact]
    [Trait("StableId", "UT-186")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void CancelHandler_FillWinsRace_RecordsFillAndDoesNotReleaseCoverageEarly() => AssertExpectedRed("UT-186", @"Cancel acknowledgment/request is not terminal truth.", boundary.CancelHandler_FillWinsRace_RecordsFillAndDoesNotReleaseCoverageEarly());

    [Fact]
    [Trait("StableId", "UT-187")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void ExternalReplaceObservation_OldOrderOrSuccessorSeen_ReconcilesWithoutCreatingIntent() => AssertExpectedRed("UT-187", @"The MVP normalizes externally produced replacement statuses but never sends PATCH or assumes a successor.", boundary.ExternalReplaceObservation_OldOrderOrSuccessorSeen_ReconcilesWithoutCreatingIntent());

    [Fact]
    [Trait("StableId", "UT-188")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void OrderChangeRequest_ConfirmedCancel_RequiresNewApprovalAndNewIntent() => AssertExpectedRed("UT-188", @"Cancel-confirm-reapprove-new-submit remains separate controlled operations with a new identity.", boundary.OrderChangeRequest_ConfirmedCancel_RequiresNewApprovalAndNewIntent());

    [Fact]
    [Trait("StableId", "UT-189")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void OrderStatus_UnknownFutureValue_RoutesToReconciliationAndAlert() => AssertExpectedRed("UT-189", @"Forward compatibility fails safe.", boundary.OrderStatus_UnknownFutureValue_RoutesToReconciliationAndAlert());

    [Fact]
    [Trait("StableId", "UT-190")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void BrokerActivity_TradeCorrectOrTradeBust_ReconcilesPositionAndCashAgain() => AssertExpectedRed("UT-190", @"Corrections create new observations and recalculate derived state.", boundary.BrokerActivity_TradeCorrectOrTradeBust_ReconcilesPositionAndCashAgain());

}
