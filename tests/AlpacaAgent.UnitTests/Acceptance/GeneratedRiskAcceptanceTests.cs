using AlpacaAgent.Application.Acceptance;
using Xunit;

namespace AlpacaAgent.UnitTests.Acceptance;

public sealed class GeneratedRiskAcceptanceTests : AcceptanceTestBase
{
    private readonly RiskFutureBoundary boundary = new();

    [Fact]
    [Trait("StableId", "UT-150")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void RiskGate_AllRulesPass_IssuesApprovalBoundToExactInputs() => AssertExpectedRed("UT-150", @"Approval contains action-set and account/market/policy/reconciliation identities plus expiry.", boundary.RiskGate_AllRulesPass_IssuesApprovalBoundToExactInputs());

    [Fact]
    [Trait("StableId", "UT-151")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void RiskGate_AnySingleRuleFails_RejectsCompleteSet() => AssertExpectedRed("UT-151", @"Theory independently fails every ordered rule.", boundary.RiskGate_AnySingleRuleFails_RejectsCompleteSet());

    [Fact]
    [Trait("StableId", "UT-152")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void RiskGate_IndividuallyValidActionsCollectivelyOvercommitShares_Rejects() => AssertExpectedRed("UT-152", @"Aggregate safety supersedes candidate validity.", boundary.RiskGate_IndividuallyValidActionsCollectivelyOvercommitShares_Rejects());

    [Fact]
    [Trait("StableId", "UT-153")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void RiskGate_CombinedConcentrationExpirationOrPortfolioLimitExceeded_Rejects() => AssertExpectedRed("UT-153", @"Aggregate exposure boundaries are covered.", boundary.RiskGate_CombinedConcentrationExpirationOrPortfolioLimitExceeded_Rejects());

    [Fact]
    [Trait("StableId", "UT-154")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void RiskGate_DuplicateOrConflictingOrderExists_Rejects() => AssertExpectedRed("UT-154", @"Broker-visible conflicts block approval.", boundary.RiskGate_DuplicateOrConflictingOrderExists_Rejects());

    [Fact]
    [Trait("StableId", "UT-155")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void RiskGate_DailyOrderLossErrorOrRetryLimitReached_RejectsNewEntry() => AssertExpectedRed("UT-155", @"Lifecycle management remains separately allowed.", boundary.RiskGate_DailyOrderLossErrorOrRetryLimitReached_RejectsNewEntry());

    [Fact]
    [Trait("StableId", "UT-156")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void RiskGate_PauseOrKillSwitchActive_Rejects() => AssertExpectedRed("UT-156", @"Every control scope is covered.", boundary.RiskGate_PauseOrKillSwitchActive_Rejects());

    [Fact]
    [Trait("StableId", "UT-157")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void Approval_QuotePositionPermissionOrReservationChanges_IsInvalid() => AssertExpectedRed("UT-157", @"Any bound state change invalidates the token.", boundary.Approval_QuotePositionPermissionOrReservationChanges_IsInvalid());

    [Fact]
    [Trait("StableId", "UT-158")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void Approval_PolicyOrComponentVersionChanges_IsInvalid() => AssertExpectedRed("UT-158", @"Rollout drift cannot reuse an old approval.", boundary.Approval_PolicyOrComponentVersionChanges_IsInvalid());

    [Fact]
    [Trait("StableId", "UT-159")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void Approval_ExactlyAtExpiry_IsInvalid() => AssertExpectedRed("UT-159", @"Time boundary is unambiguous.", boundary.Approval_ExactlyAtExpiry_IsInvalid());

    [Fact]
    [Trait("StableId", "UT-160")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void Approval_ActionSetReorderedCanonically_SameHashButChangedActionDifferentHash() => AssertExpectedRed("UT-160", @"Canonical ordering is stable; material mutation is detected.", boundary.Approval_ActionSetReorderedCanonically_SameHashButChangedActionDifferentHash());

    [Fact]
    [Trait("StableId", "UT-161")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void RiskGate_Rejection_StoresEveryRuleResultInStableOrder() => AssertExpectedRed("UT-161", @"Evaluation is complete and auditable, not short-circuited invisibly.", boundary.RiskGate_Rejection_StoresEveryRuleResultInStableOrder());

    [Fact]
    [Trait("StableId", "UT-162")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void PreSubmitCheck_KillSwitchActivatesAfterApproval_BlocksExternalWrite() => AssertExpectedRed("UT-162", @"Final control state wins.", boundary.PreSubmitCheck_KillSwitchActivatesAfterApproval_BlocksExternalWrite());

    [Fact]
    [Trait("StableId", "UT-163")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void PreSubmitCheck_QuoteBecomesStale_BlocksAndRequiresRevalidation() => AssertExpectedRed("UT-163", @"Old approval is not patched.", boundary.PreSubmitCheck_QuoteBecomesStale_BlocksAndRequiresRevalidation());

    [Fact]
    [Trait("StableId", "UT-164")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void ConcurrentReservation_LoserCannotProduceReadyIntent() => AssertExpectedRed("UT-164", @"Application handles failed reservation as safe re-reconciliation; database atomicity is later integration-tested.", boundary.ConcurrentReservation_LoserCannotProduceReadyIntent());

}
