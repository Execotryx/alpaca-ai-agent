using Xunit;

namespace AlpacaAgent.UnitTests.Acceptance;

public sealed class GeneratedFeatureAndEvidenceAcceptanceTests : AcceptanceTestBase
{
    [Fact]
    [Trait("StableId", "UT-080")]
    public void FeatureCalculator_FrozenObservations_ProducesGoldenFeatureVector() => AssertImplemented("UT-080", @"Pure calculations reproduce the approved golden vector.");

    [Fact]
    [Trait("StableId", "UT-081")]
    public void FeatureCalculator_FutureObservation_IsExcluded() => AssertImplemented("UT-081", @"No observation available after decision time enters a feature.");

    [Fact]
    [Trait("StableId", "UT-082")]
    public void FeatureCalculator_InsufficientLookback_ReturnsUnavailableNotZero() => AssertImplemented("UT-082", @"Missing history is explicit.");

    [Fact]
    [Trait("StableId", "UT-083")]
    public void OptionMetrics_KnownInputs_ProduceGoldenPremiumYieldBreakevenAndUpside() => AssertImplemented("UT-083", @"Decimal conversions and rounding match the declared formula.");

    [Fact]
    [Trait("StableId", "UT-084")]
    public void OptionMetrics_ThresholdMatrix_UsesDeclaredBoundarySemantics() => AssertImplemented("UT-084", @"DTE, strike, spread, volume, open-interest, delta, and premium limits are tested just below, at, and above bounds.");

    [Fact]
    [Trait("StableId", "UT-085")]
    public void EvidenceBuilder_ApprovedFreshSources_ProducesDeterministicNeutralBundle() => AssertImplemented("UT-085", @"Same inputs yield the same evidence IDs, order, hash, and deterministic scope flags without semantic supporting/opposing labels.");

    [Fact]
    [Trait("StableId", "UT-086")]
    public void EvidenceBuilder_DuplicateSyndicatedStory_KeepsCanonicalEvidenceOnce() => AssertImplemented("UT-086", @"Duplicate content does not overweight one claim.");

    [Fact]
    [Trait("StableId", "UT-087")]
    public void EvidenceBuilder_FutureOrStaleEvidence_ExcludesOrMarksPerPolicy() => AssertImplemented("UT-087", @"Decision-time and recency rules are explicit.");

    [Fact]
    [Trait("StableId", "UT-088")]
    public void EvidenceBuilder_UnavailablePaywalledOrInvalidBody_PreservesMissingness() => AssertImplemented("UT-088", @"Missing body is not synthesized.");

    [Fact]
    [Trait("StableId", "UT-089")]
    public void EvidenceBuilder_OversizedBody_TruncatesAtDeterministicSafeBoundary() => AssertImplemented("UT-089", @"Size caps preserve provenance and a truncation marker.");

    [Fact]
    [Trait("StableId", "UT-090")]
    public void EvidenceBuilder_InstructionLikeText_RemainsUntrustedEvidence() => AssertImplemented("UT-090", @"Prompt-like text never enters trusted instructions.");

    [Fact]
    [Trait("StableId", "UT-091")]
    public void EvidenceBuilder_DistinctClaimsFromDifferentSources_PreservesBothNeutralRecords() => AssertImplemented("UT-091", @"Deterministic bundling does not remove distinct source claims; the AI assessment, not the builder, owns semantic conflict classification.");

    [Fact]
    [Trait("StableId", "UT-092")]
    public void EvidenceBuilder_UnrelatedTickerMention_DoesNotExpandSymbolScope() => AssertImplemented("UT-092", @"Evidence cannot silently add a tradable symbol.");

    [Fact]
    [Trait("StableId", "UT-093")]
    public void PolicyInputPackage_EvidenceOrderCanonicalization_IsStable() => AssertImplemented("UT-093", @"Input hash is stable under source-return order changes after canonicalization.");

}
