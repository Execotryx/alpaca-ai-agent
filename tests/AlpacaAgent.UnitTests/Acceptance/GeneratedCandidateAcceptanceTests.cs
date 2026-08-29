using AlpacaAgent.Application.Acceptance;
using Xunit;

namespace AlpacaAgent.UnitTests.Acceptance;

public sealed class GeneratedCandidateAcceptanceTests : AcceptanceTestBase
{
    private readonly CandidateFutureBoundary boundary = new();

    [Fact]
    [Trait("StableId", "UT-132")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void CandidateGenerator_SkipAbstentionOrAgentFailure_ReturnsEmpty() => AssertExpectedRed("UT-132", @"No executable candidate is produced for any non-call outcome.", boundary.CandidateGenerator_SkipAbstentionOrAgentFailure_ReturnsEmpty());

    [Fact]
    [Trait("StableId", "UT-133")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void CandidateGenerator_OneFreeBlock_ProducesAtMostOneAllocationPerBlock() => AssertExpectedRed("UT-133", @"Coverage references are explicit.", boundary.CandidateGenerator_OneFreeBlock_ProducesAtMostOneAllocationPerBlock());

    [Fact]
    [Trait("StableId", "UT-134")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void CandidateGenerator_AdjustedInactiveOrUnknownContract_IsRejected() => AssertExpectedRed("UT-134", @"Only active standard MVP contracts qualify.", boundary.CandidateGenerator_AdjustedInactiveOrUnknownContract_IsRejected());

    [Fact]
    [Trait("StableId", "UT-135")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void CandidateGenerator_DuplicatePendingActionForContract_IsRejected() => AssertExpectedRed("UT-135", @"Existing/pending exposure prevents duplicate action.", boundary.CandidateGenerator_DuplicatePendingActionForContract_IsRejected());

    [Fact]
    [Trait("StableId", "UT-136")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void CandidateAdmissibility_BoundaryMatrix_MatchesFrozenProfile() => AssertExpectedRed("UT-136", @"Expiry, strike, delta, spread, liquidity, event, and quote boundaries are exhaustive.", boundary.CandidateAdmissibility_BoundaryMatrix_MatchesFrozenProfile());

    [Fact]
    [Trait("StableId", "UT-137")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void CandidateAdmissibility_InvalidMetric_IsRejectedNotRankedLast() => AssertExpectedRed("UT-137", @"Invalid data cannot survive as a low score.", boundary.CandidateAdmissibility_InvalidMetric_IsRejectedNotRankedLast());

    [Fact]
    [Trait("StableId", "UT-138")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void Ranker_GoldenCandidates_ProducesGoldenScoresAndOrder() => AssertExpectedRed("UT-138", @"Same candidates and versions reproduce exact ranking.", boundary.Ranker_GoldenCandidates_ProducesGoldenScoresAndOrder());

    [Fact]
    [Trait("StableId", "UT-139")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void Ranker_InputOrderOrCultureChanges_ResultIsUnchanged() => AssertExpectedRed("UT-139", @"Stable sort and invariant parsing eliminate environmental drift.", boundary.Ranker_InputOrderOrCultureChanges_ResultIsUnchanged());

    [Fact]
    [Trait("StableId", "UT-140")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void Ranker_EqualScores_UsesDeclaredTieBreakChain() => AssertExpectedRed("UT-140", @"Every tie-break field is covered in order.", boundary.Ranker_EqualScores_UsesDeclaredTieBreakChain());

    [Fact]
    [Trait("StableId", "UT-141")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void Selector_TwoCandidatesShareOneCoverageBlock_SelectsOnlyWinner() => AssertExpectedRed("UT-141", @"One share block cannot be allocated twice.", boundary.Selector_TwoCandidatesShareOneCoverageBlock_SelectsOnlyWinner());

    [Fact]
    [Trait("StableId", "UT-142")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void Selector_ZeroAdmissibleCandidates_ReturnsNoTradeWithReasons() => AssertExpectedRed("UT-142", @"Empty result is explicit and auditable.", boundary.Selector_ZeroAdmissibleCandidates_ReturnsNoTradeWithReasons());

    [Fact]
    [Trait("StableId", "UT-143")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void Selector_IdenticalInputs_RepeatedRuns_ProduceSameActionSetHash() => AssertExpectedRed("UT-143", @"Selection is deterministic.", boundary.Selector_IdenticalInputs_RepeatedRuns_ProduceSameActionSetHash());

    [Fact]
    [Trait("StableId", "UT-144")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void Selector_AblationDiffersFromAiPolicy_RecordsMaterialContribution() => AssertExpectedRed("UT-144", @"Difference is reported, not used as a silent fallback.", boundary.Selector_AblationDiffersFromAiPolicy_RecordsMaterialContribution());

    [Fact]
    [Trait("StableId", "UT-145")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void Selector_ActionCountBoundary_NeverExceedsCycleLimit() => AssertExpectedRed("UT-145", @"Exact maximum and one-above cases are tested.", boundary.Selector_ActionCountBoundary_NeverExceedsCycleLimit());

}
