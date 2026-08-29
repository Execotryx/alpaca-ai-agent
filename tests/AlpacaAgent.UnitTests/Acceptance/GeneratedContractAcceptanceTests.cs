using Xunit;

namespace AlpacaAgent.UnitTests.Acceptance;

public sealed class GeneratedContractAcceptanceTests : AcceptanceTestBase
{
    [Fact]
    [Trait("StableId", "UT-001")]
    public void Quantity_CreateFractionalOptionContracts_IsRejected() => AssertImplemented("UT-001", @"Option quantities cannot be fractional.");

    [Fact]
    [Trait("StableId", "UT-002")]
    public void Coverage_FromFractionalShares_CountsOnlyCompleteHundreds() => AssertImplemented("UT-002", @"`99.999` yields zero blocks; `100.999` yields one.");

    [Fact]
    [Trait("StableId", "UT-003")]
    public void OptionalValue_UnknownInvalidUnavailableAndZero_RemainDistinct() => AssertImplemented("UT-003", @"Four states survive mapping and JSON round-trip without coercion.");

    [Fact]
    [Trait("StableId", "UT-004")]
    public void Money_RoundAtDeclaredBoundary_UsesConfiguredRuleOnce() => AssertImplemented("UT-004", @"Money is rounded exactly once at the declared boundary.");

    [Fact]
    [Trait("StableId", "UT-005")]
    public void Ratio_DivideByZero_ReturnsInvalidNotInfinity() => AssertImplemented("UT-005", @"Invalid arithmetic cannot become a rankable value.");

    [Fact]
    [Trait("StableId", "UT-006")]
    public void ContractSchema_UnknownProperty_IsRejected() => AssertImplemented("UT-006", @"`additionalProperties: false` is enforced.");

    [Fact]
    [Trait("StableId", "UT-007")]
    public void ContractSchema_MissingRequiredProperty_IsRejected() => AssertImplemented("UT-007", @"Missingness is not filled with a default.");

    [Fact]
    [Trait("StableId", "UT-008")]
    public void ContractSchema_UnknownEnumValue_IsRejectedOrMigratedExplicitly() => AssertImplemented("UT-008", @"No permissive enum fallback enters Domain.");

    [Fact]
    [Trait("StableId", "UT-009")]
    public void CanonicalHasher_PropertyAndDictionaryOrderVaries_HashIsStable() => AssertImplemented("UT-009", @"Semantically identical records produce the same hash.");

    [Fact]
    [Trait("StableId", "UT-010")]
    public void CanonicalHasher_MaterialValueChanges_HashChanges() => AssertImplemented("UT-010", @"Any executable or approval-bound value changes identity.");

    [Fact]
    [Trait("StableId", "UT-011")]
    public void DecisionTime_ParseDifferentOffsets_RepresentsSameInstant() => AssertImplemented("UT-011", @"Offset-equivalent timestamps normalize to one UTC instant.");

    [Fact]
    [Trait("StableId", "UT-012")]
    public void PersistedContract_OlderSupportedVersion_UsesExplicitMigrator() => AssertImplemented("UT-012", @"Supported old data is migrated deliberately; unsupported versions fail visibly.");

}
