using AlpacaAgent.Application.Acceptance;
using Xunit;

namespace AlpacaAgent.UnitTests.Acceptance;

public sealed class GeneratedFeatureAndEvidenceAcceptanceTests : AcceptanceTestBase
{
    private readonly FeatureAndEvidenceFutureBoundary boundary = new();

    [Fact]
    [Trait("StableId", "UT-080")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void FeatureCalculator_FrozenObservations_ProducesGoldenFeatureVector() => AssertExpectedRed("UT-080", @"Pure calculations reproduce the approved golden vector.", boundary.FeatureCalculator_FrozenObservations_ProducesGoldenFeatureVector());

    [Fact]
    [Trait("StableId", "UT-081")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void FeatureCalculator_FutureObservation_IsExcluded() => AssertExpectedRed("UT-081", @"No observation available after decision time enters a feature.", boundary.FeatureCalculator_FutureObservation_IsExcluded());

    [Fact]
    [Trait("StableId", "UT-082")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void FeatureCalculator_InsufficientLookback_ReturnsUnavailableNotZero() => AssertExpectedRed("UT-082", @"Missing history is explicit.", boundary.FeatureCalculator_InsufficientLookback_ReturnsUnavailableNotZero());

    [Fact]
    [Trait("StableId", "UT-083")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void OptionMetrics_KnownInputs_ProduceGoldenPremiumYieldBreakevenAndUpside() => AssertExpectedRed("UT-083", @"Decimal conversions and rounding match the declared formula.", boundary.OptionMetrics_KnownInputs_ProduceGoldenPremiumYieldBreakevenAndUpside());

    [Fact]
    [Trait("StableId", "UT-084")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void OptionMetrics_ThresholdMatrix_UsesDeclaredBoundarySemantics() => AssertExpectedRed("UT-084", @"DTE, strike, spread, volume, open-interest, delta, and premium limits are tested just below, at, and above bounds.", boundary.OptionMetrics_ThresholdMatrix_UsesDeclaredBoundarySemantics());

    [Fact]
    [Trait("StableId", "UT-085")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void EvidenceBuilder_ApprovedFreshSources_ProducesDeterministicNeutralBundle() => AssertExpectedRed("UT-085", @"Same inputs yield the same evidence IDs, order, hash, and deterministic scope flags without semantic supporting/opposing labels.", boundary.EvidenceBuilder_ApprovedFreshSources_ProducesDeterministicNeutralBundle());

    [Fact]
    [Trait("StableId", "UT-086")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void EvidenceBuilder_DuplicateSyndicatedStory_KeepsCanonicalEvidenceOnce() => AssertExpectedRed("UT-086", @"Duplicate content does not overweight one claim.", boundary.EvidenceBuilder_DuplicateSyndicatedStory_KeepsCanonicalEvidenceOnce());

    [Fact]
    [Trait("StableId", "UT-087")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void EvidenceBuilder_FutureOrStaleEvidence_ExcludesOrMarksPerPolicy() => AssertExpectedRed("UT-087", @"Decision-time and recency rules are explicit.", boundary.EvidenceBuilder_FutureOrStaleEvidence_ExcludesOrMarksPerPolicy());

    [Fact]
    [Trait("StableId", "UT-088")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void EvidenceBuilder_UnavailablePaywalledOrInvalidBody_PreservesMissingness() => AssertExpectedRed("UT-088", @"Missing body is not synthesized.", boundary.EvidenceBuilder_UnavailablePaywalledOrInvalidBody_PreservesMissingness());

    [Fact]
    [Trait("StableId", "UT-089")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void EvidenceBuilder_OversizedBody_TruncatesAtDeterministicSafeBoundary() => AssertExpectedRed("UT-089", @"Size caps preserve provenance and a truncation marker.", boundary.EvidenceBuilder_OversizedBody_TruncatesAtDeterministicSafeBoundary());

    [Fact]
    [Trait("StableId", "UT-090")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void EvidenceBuilder_InstructionLikeText_RemainsUntrustedEvidence() => AssertExpectedRed("UT-090", @"Prompt-like text never enters trusted instructions.", boundary.EvidenceBuilder_InstructionLikeText_RemainsUntrustedEvidence());

    [Fact]
    [Trait("StableId", "UT-091")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void EvidenceBuilder_DistinctClaimsFromDifferentSources_PreservesBothNeutralRecords() => AssertExpectedRed("UT-091", @"Deterministic bundling does not remove distinct source claims; the AI assessment, not the builder, owns semantic conflict classification.", boundary.EvidenceBuilder_DistinctClaimsFromDifferentSources_PreservesBothNeutralRecords());

    [Fact]
    [Trait("StableId", "UT-092")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void EvidenceBuilder_UnrelatedTickerMention_DoesNotExpandSymbolScope() => AssertExpectedRed("UT-092", @"Evidence cannot silently add a tradable symbol.", boundary.EvidenceBuilder_UnrelatedTickerMention_DoesNotExpandSymbolScope());

    [Fact]
    [Trait("StableId", "UT-093")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void PolicyInputPackage_EvidenceOrderCanonicalization_IsStable() => AssertExpectedRed("UT-093", @"Input hash is stable under source-return order changes after canonicalization.", boundary.PolicyInputPackage_EvidenceOrderCanonicalization_IsStable());

}
