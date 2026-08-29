using AlpacaAgent.Application.Acceptance;
using Xunit;

namespace AlpacaAgent.UnitTests.Acceptance;

public sealed class GeneratedBrokerAndMarketAcceptanceTests : AcceptanceTestBase
{
    private readonly BrokerAndMarketFutureBoundary boundary = new();

    [Fact]
    [Trait("StableId", "UT-060")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void Freshness_SourceTimeAtBoundary_UsesDocumentedInclusiveRule() => AssertExpectedRed("UT-060", @"Exactly-at-TTL behavior is fixed and tested; retrieval time does not replace source time.", boundary.Freshness_SourceTimeAtBoundary_UsesDocumentedInclusiveRule());

    [Fact]
    [Trait("StableId", "UT-061")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void Freshness_SourceOldButRetrievedNow_IsStale() => AssertExpectedRed("UT-061", @"A new download timestamp cannot freshen an old quote.", boundary.Freshness_SourceOldButRetrievedNow_IsStale());

    [Fact]
    [Trait("StableId", "UT-062")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void QuoteQuality_MissingZeroNegativeCrossedOrInvalidQuote_IsRejected() => AssertExpectedRed("UT-062", @"Theory covers each invalid bid/ask case with a stable reason.", boundary.QuoteQuality_MissingZeroNegativeCrossedOrInvalidQuote_IsRejected());

    [Fact]
    [Trait("StableId", "UT-063")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void QuoteQuality_LockedQuote_FollowsFrozenPolicy() => AssertExpectedRed("UT-063", @"The exact locked-market rule is explicit, not accidental.", boundary.QuoteQuality_LockedQuote_FollowsFrozenPolicy());

    [Fact]
    [Trait("StableId", "UT-064")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void Snapshot_OutOfOrderObservation_DoesNotReplaceNewerSourceTruth() => AssertExpectedRed("UT-064", @"Timestamp regression is ignored and audited.", boundary.Snapshot_OutOfOrderObservation_DoesNotReplaceNewerSourceTruth());

    [Fact]
    [Trait("StableId", "UT-065")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void OptionChain_MissingPageTokenCompletion_IsIncomplete() => AssertExpectedRed("UT-065", @"Partial pagination cannot masquerade as a complete universe.", boundary.OptionChain_MissingPageTokenCompletion_IsIncomplete());

    [Fact]
    [Trait("StableId", "UT-066")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void MarketFeed_IndicativeWhenOpraRequired_IsPolicyIneligible() => AssertExpectedRed("UT-066", @"Feed downgrade is visible and blocks the affected decision.", boundary.MarketFeed_IndicativeWhenOpraRequired_IsPolicyIneligible());

    [Fact]
    [Trait("StableId", "UT-067")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void MarketSession_HolidayEarlyCloseOrDst_UsesClockAndCalendarInstant() => AssertExpectedRed("UT-067", @"Theory uses real offset/early-close fixtures and no machine-local assumption.", boundary.MarketSession_HolidayEarlyCloseOrDst_UsesClockAndCalendarInstant());

    [Fact]
    [Trait("StableId", "UT-068")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void Account_OptionsLevelBelowOne_IsIneligible() => AssertExpectedRed("UT-068", @"Covered-call entry requires the frozen minimum permission.", boundary.Account_OptionsLevelBelowOne_IsIneligible());

    [Fact]
    [Trait("StableId", "UT-069")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void Account_TradeBlockedOrBuyingPowerInsufficient_IsIneligible() => AssertExpectedRed("UT-069", @"Account restriction wins over candidate attractiveness.", boundary.Account_TradeBlockedOrBuyingPowerInsufficient_IsIneligible());

    [Fact]
    [Trait("StableId", "UT-070")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void Reconciler_NinetyNineShares_NoCoverage() => AssertExpectedRed("UT-070", @"No contract is available.", boundary.Reconciler_NinetyNineShares_NoCoverage());

    [Fact]
    [Trait("StableId", "UT-071")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void Reconciler_OneHundredFreeShares_OneCoverageBlock() => AssertExpectedRed("UT-071", @"Exactly one standard contract may be covered.", boundary.Reconciler_OneHundredFreeShares_OneCoverageBlock());

    [Fact]
    [Trait("StableId", "UT-072")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void Reconciler_TwoHundredSharesAndOneShortCall_OneFreeBlock() => AssertExpectedRed("UT-072", @"Existing obligations reduce free coverage.", boundary.Reconciler_TwoHundredSharesAndOneShortCall_OneFreeBlock());

    [Fact]
    [Trait("StableId", "UT-073")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void Reconciler_PendingShareSaleAndOptionOrders_ReserveCoverageOnce() => AssertExpectedRed("UT-073", @"Reservations are deduplicated and subtracted once.", boundary.Reconciler_PendingShareSaleAndOptionOrders_ReserveCoverageOnce());

    [Fact]
    [Trait("StableId", "UT-074")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void Reconciler_NonStandardMultiplierOrAdjustedDeliverable_ManagementOnly() => AssertExpectedRed("UT-074", @"Adjusted contracts do not enter the standard 100-share MVP path.", boundary.Reconciler_NonStandardMultiplierOrAdjustedDeliverable_ManagementOnly());

    [Fact]
    [Trait("StableId", "UT-075")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void Reconciler_ConflictingPositionOrderAndActivitySnapshots_SafeHalts() => AssertExpectedRed("UT-075", @"Inconsistent broker truth cannot be resolved optimistically.", boundary.Reconciler_ConflictingPositionOrderAndActivitySnapshots_SafeHalts());

    [Fact]
    [Trait("StableId", "UT-076")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void Reconciler_DuplicateBrokerObservation_DoesNotDoubleCount() => AssertExpectedRed("UT-076", @"Duplicate order/activity payloads are idempotent.", boundary.Reconciler_DuplicateBrokerObservation_DoesNotDoubleCount());

    [Fact]
    [Trait("StableId", "UT-077")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void CorporateAction_AffectsUnderlyingOrContract_InvalidatesEligibility() => AssertExpectedRed("UT-077", @"Split, merger, symbol change, or special-dividend fixtures pause new entries.", boundary.CorporateAction_AffectsUnderlyingOrContract_InvalidatesEligibility());

    [Fact]
    [Trait("StableId", "UT-078")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void MarketHalt_OptionEntryRequested_IsRejectedWithMarketStateReason() => AssertExpectedRed("UT-078", @"Halt/pause state blocks new options exposure.", boundary.MarketHalt_OptionEntryRequested_IsRejectedWithMarketStateReason());

}
