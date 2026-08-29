using AlpacaAgent.Application.Acceptance;
using Xunit;

namespace AlpacaAgent.UnitTests.Acceptance;

public sealed class GeneratedPolicyAcceptanceTests : AcceptanceTestBase
{
    private readonly PolicyFutureBoundary boundary = new();

    [Fact]
    [Trait("StableId", "UT-100")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void PolicyValidator_AllowedCallPoliciesWithValidEvidence_AreAccepted() => AssertExpectedRed("UT-100", @"Theory covers conservative, balanced, and income policies.", boundary.PolicyValidator_AllowedCallPoliciesWithValidEvidence_AreAccepted());

    [Fact]
    [Trait("StableId", "UT-101")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void PolicyValidator_ValidSkip_IsAcceptedAsNoTrade() => AssertExpectedRed("UT-101", @"`SKIP` is completed, not failed.", boundary.PolicyValidator_ValidSkip_IsAcceptedAsNoTrade());

    [Fact]
    [Trait("StableId", "UT-102")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void PolicyValidator_ValidInsufficientInformation_IsAcceptedAsAbstention() => AssertExpectedRed("UT-102", @"Required missing-information evidence is present.", boundary.PolicyValidator_ValidInsufficientInformation_IsAcceptedAsAbstention());

    [Fact]
    [Trait("StableId", "UT-103")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void PolicyValidator_TimeoutRefusalRateLimitOrTruncation_IsOperationalFailure() => AssertExpectedRed("UT-103", @"Theory proves none is converted to abstention.", boundary.PolicyValidator_TimeoutRefusalRateLimitOrTruncation_IsOperationalFailure());

    [Fact]
    [Trait("StableId", "UT-104")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void PolicyValidator_MalformedJsonOrMissingRequiredField_IsRejected() => AssertExpectedRed("UT-104", @"Strict schema failure is classified.", boundary.PolicyValidator_MalformedJsonOrMissingRequiredField_IsRejected());

    [Fact]
    [Trait("StableId", "UT-105")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void PolicyValidator_AdditionalProperty_IsRejected() => AssertExpectedRed("UT-105", @"Extra fields never survive deserialization/validation.", boundary.PolicyValidator_AdditionalProperty_IsRejected());

    [Fact]
    [Trait("StableId", "UT-106")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void PolicyValidator_UnknownPolicySymbolOrEvidenceId_IsRejected() => AssertExpectedRed("UT-106", @"All three allowlists are enforced.", boundary.PolicyValidator_UnknownPolicySymbolOrEvidenceId_IsRejected());

    [Fact]
    [Trait("StableId", "UT-107")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void PolicyValidator_ExecutableFieldPresent_IsRejected() => AssertExpectedRed("UT-107", @"Contract, strike, expiry, quantity, side, price, TIF, and broker-operation cases are covered.", boundary.PolicyValidator_ExecutableFieldPresent_IsRejected());

    [Fact]
    [Trait("StableId", "UT-108")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void PolicyValidator_InputHashOrSchemaVersionMismatch_IsRejected() => AssertExpectedRed("UT-108", @"Output cannot bind to a different input/configuration.", boundary.PolicyValidator_InputHashOrSchemaVersionMismatch_IsRejected());

    [Fact]
    [Trait("StableId", "UT-109")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void PolicyValidator_MaterialClaimWithoutEvidence_IsRejected() => AssertExpectedRed("UT-109", @"Every required material claim has supplied evidence support.", boundary.PolicyValidator_MaterialClaimWithoutEvidence_IsRejected());

    [Fact]
    [Trait("StableId", "UT-110")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void PolicyValidator_DuplicateEvidenceReferences_DoNotIncreaseSupport() => AssertExpectedRed("UT-110", @"Duplicate IDs are normalized or rejected per schema.", boundary.PolicyValidator_DuplicateEvidenceReferences_DoNotIncreaseSupport());

    [Fact]
    [Trait("StableId", "UT-111")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void PolicyValidator_ContradictionOmittedWhenInputsConflict_IsRejected() => AssertExpectedRed("UT-111", @"Frozen conflict fixtures require explicit conflict treatment.", boundary.PolicyValidator_ContradictionOmittedWhenInputsConflict_IsRejected());

    [Fact]
    [Trait("StableId", "UT-112")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void PolicyValidator_LowOrContradictoryConfidence_FollowsFrozenRule() => AssertExpectedRed("UT-112", @"Confidence cannot contradict policy/uncertainty requirements.", boundary.PolicyValidator_LowOrContradictoryConfidence_FollowsFrozenRule());

    [Fact]
    [Trait("StableId", "UT-113")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void PolicyValidator_ToolRequestOrToolCall_IsCapabilityViolation() => AssertExpectedRed("UT-113", @"Any tool behavior is rejected and recorded.", boundary.PolicyValidator_ToolRequestOrToolCall_IsCapabilityViolation());

    [Fact]
    [Trait("StableId", "UT-114")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void PolicyValidator_SessionThreadOrPreviousResponseIdentifier_IsCapabilityViolation() => AssertExpectedRed("UT-114", @"Stateful metadata is prohibited.", boundary.PolicyValidator_SessionThreadOrPreviousResponseIdentifier_IsCapabilityViolation());

    [Fact]
    [Trait("StableId", "UT-115")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void PolicyValidator_ResolvedModelDiffersFromFrozenModel_IsRejected() => AssertExpectedRed("UT-115", @"Silent model/provider substitution blocks normal execution.", boundary.PolicyValidator_ResolvedModelDiffersFromFrozenModel_IsRejected());

    [Fact]
    [Trait("StableId", "UT-116")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void PolicyAssessor_CancellationAndTimeout_ArePropagatedAndBounded() => AssertExpectedRed("UT-116", @"Fake provider observes cancellation; attempt is classified once.", boundary.PolicyAssessor_CancellationAndTimeout_ArePropagatedAndBounded());

    [Fact]
    [Trait("StableId", "UT-117")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void PolicyAttempt_RetryCreatesImmutableStartAndResultAndPreservesPriorFailure() => AssertExpectedRed("UT-117", @"Attempt 1 start/result remain unchanged; retry creates attempt 2 records and a late result cannot replace the authoritative workflow outcome.", boundary.PolicyAttempt_RetryCreatesImmutableStartAndResultAndPreservesPriorFailure());

    [Fact]
    [Trait("StableId", "UT-118")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void PolicyAssessment_ReorderedOrDuplicatedEvidenceFixture_ProducesStabilityMeasurement() => AssertExpectedRed("UT-118", @"Variation is recorded without relaxing deterministic safety.", boundary.PolicyAssessment_ReorderedOrDuplicatedEvidenceFixture_ProducesStabilityMeasurement());

    [Fact]
    [Trait("StableId", "UT-119")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void PolicyAssessment_PromptInjectionFixture_CannotChangeCapabilityOrPolicySet() => AssertExpectedRed("UT-119", @"Retrieved instructions do not alter trusted rules.", boundary.PolicyAssessment_PromptInjectionFixture_CannotChangeCapabilityOrPolicySet());

    [Fact]
    [Trait("StableId", "UT-120")]
    [Trait("GateStatus", "ExpectedRedFuture")]
    public void PolicyValidator_OutputAtSizeLimitAcceptedAndAboveLimitRejected() => AssertExpectedRed("UT-120", @"The exact output bound is enforced.", boundary.PolicyValidator_OutputAtSizeLimitAcceptedAndAboveLimitRejected());

}
