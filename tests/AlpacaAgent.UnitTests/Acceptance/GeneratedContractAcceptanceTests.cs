using System.Text.Json;
using AlpacaAgent.Domain.Contracts;
using Xunit;

namespace AlpacaAgent.UnitTests.Acceptance;

public sealed class GeneratedContractAcceptanceTests
{
    [Fact, Trait("StableId", "UT-001"), Trait("GateStatus", "ActiveThroughPhase3")]
    public void Quantity_CreateFractionalOptionContracts_IsRejected() => Assert.Throws<ArgumentOutOfRangeException>(() => OptionQuantity.Create(1.5m));
    [Fact, Trait("StableId", "UT-002"), Trait("GateStatus", "ActiveThroughPhase3")]
    public void Coverage_FromFractionalShares_CountsOnlyCompleteHundreds() { Assert.Equal(0, Coverage.CompleteStandardBlocks(99.999m)); Assert.Equal(1, Coverage.CompleteStandardBlocks(100.999m)); }
    [Fact, Trait("StableId", "UT-003"), Trait("GateStatus", "ActiveThroughPhase3")]
    public void OptionalValue_UnknownInvalidUnavailableAndZero_RemainDistinct()
    {
        ObservedValue<decimal>[] values = [new ObservedValue<decimal>.Known(0m), new ObservedValue<decimal>.Unknown(), new ObservedValue<decimal>.Unavailable(), new ObservedValue<decimal>.Invalid("BAD")];
        var copy = JsonSerializer.Deserialize<ObservedValue<decimal>[]>(JsonSerializer.Serialize(values))!;
        Assert.IsType<ObservedValue<decimal>.Known>(copy[0]); Assert.IsType<ObservedValue<decimal>.Unknown>(copy[1]); Assert.IsType<ObservedValue<decimal>.Unavailable>(copy[2]); Assert.IsType<ObservedValue<decimal>.Invalid>(copy[3]);
    }
    [Fact, Trait("StableId", "UT-004"), Trait("GateStatus", "ActiveThroughPhase3")]
    public void Money_RoundAtDeclaredBoundary_UsesConfiguredRuleOnce() => Assert.Equal(1.02m, MoneyMath.Round(1.025m, 2, MidpointRounding.ToEven));
    [Fact, Trait("StableId", "UT-005"), Trait("GateStatus", "ActiveThroughPhase3")]
    public void Ratio_DivideByZero_ReturnsInvalidNotInfinity() => Assert.Equal("DIVIDE_BY_ZERO", Assert.IsType<ObservedValue<decimal>.Invalid>(RatioMath.Divide(1, 0)).Reason);
    private static readonly ContractShape Shape = new(new HashSet<string>(["status", "id"], StringComparer.Ordinal), new Dictionary<string, IReadOnlySet<string>> { ["status"] = new HashSet<string>(["OPEN", "CLOSED"], StringComparer.Ordinal) });
    [Fact, Trait("StableId", "UT-006"), Trait("GateStatus", "ActiveThroughPhase3")]
    public void ContractSchema_UnknownProperty_IsRejected() => Assert.Contains("UNKNOWN_PROPERTY:extra", Validate("""{"id":"1","status":"OPEN","extra":true}"""));
    [Fact, Trait("StableId", "UT-007"), Trait("GateStatus", "ActiveThroughPhase3")]
    public void ContractSchema_MissingRequiredProperty_IsRejected() => Assert.Contains("MISSING_REQUIRED:id", Validate("""{"status":"OPEN"}"""));
    [Fact, Trait("StableId", "UT-008"), Trait("GateStatus", "ActiveThroughPhase3")]
    public void ContractSchema_UnknownEnumValue_IsRejectedOrMigratedExplicitly() => Assert.Contains("UNKNOWN_ENUM:status", Validate("""{"id":"1","status":"FUTURE"}"""));
    [Fact, Trait("StableId", "UT-009"), Trait("GateStatus", "ActiveThroughPhase3")]
    public void CanonicalHasher_PropertyAndDictionaryOrderVaries_HashIsStable() => Assert.Equal(Hash("""{"a":1,"b":{"x":2,"y":3}}"""), Hash("""{"b":{"y":3,"x":2},"a":1}"""));
    [Fact, Trait("StableId", "UT-010"), Trait("GateStatus", "ActiveThroughPhase3")]
    public void CanonicalHasher_MaterialValueChanges_HashChanges() => Assert.NotEqual(Hash("""{"quantity":1}"""), Hash("""{"quantity":2}"""));
    [Fact, Trait("StableId", "UT-011"), Trait("GateStatus", "ActiveThroughPhase3")]
    public void DecisionTime_ParseDifferentOffsets_RepresentsSameInstant() => Assert.Equal(UtcInstant.Parse("2026-08-29T10:00:00Z"), UtcInstant.Parse("2026-08-29T15:00:00+05:00"));
    [Fact, Trait("StableId", "UT-012"), Trait("GateStatus", "ActiveThroughPhase3")]
    public void PersistedContract_OlderSupportedVersion_UsesExplicitMigrator()
    {
        var registry = new ContractVersionRegistry(); registry.Register("sample", 1, payload => payload);
        using var document = JsonDocument.Parse("{}");
        Assert.Equal(JsonValueKind.Object, registry.Migrate("sample", 1, 2, document.RootElement).ValueKind);
        Assert.Throws<NotSupportedException>(() => registry.Migrate("sample", 0, 2, document.RootElement));
    }
    private static IReadOnlyList<string> Validate(string json) { using var doc = JsonDocument.Parse(json); return StrictContractValidator.Validate(doc.RootElement, Shape); }
    private static string Hash(string json) { using var doc = JsonDocument.Parse(json); return CanonicalHasher.Sha256(doc.RootElement); }
}
