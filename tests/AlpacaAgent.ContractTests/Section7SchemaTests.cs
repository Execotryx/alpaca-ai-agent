using System.Text.Json;
using System.Text.Json.Nodes;
using AlpacaAgent.Domain.Contracts;
using Json.Schema;

namespace AlpacaAgent.ContractTests;

public sealed class Section7SchemaTests
{
    private static readonly object SchemaLock = new();
    private static readonly Dictionary<string, JsonSchema> Schemas = new(StringComparer.Ordinal);
    [Fact]
    public void ContractSchemas_AllObjectShapesAreClosedAndVersioned()
    {
        var files = Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "schemas"), "*.schema.json");
        Assert.Equal(10, files.Length);
        foreach (var file in files)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(file));
            AssertStrictObjects(document.RootElement, Path.GetFileName(file));
            Assert.Equal("1.0", document.RootElement.GetProperty("properties").GetProperty("schemaVersion").GetProperty("const").GetString());
        }
    }

    [Fact]
    public void PolicyAssessment_SourceGeneratedRoundTrip_ValidatesAgainstStrictSchema()
    {
        var assessment = Assessment();
        var json = JsonSerializer.Serialize(assessment, Section7JsonContext.Default.PolicyAssessmentV1Contract);
        AssertValid("policy-assessment.schema.json", json);
        var copy = JsonSerializer.Deserialize(json, Section7JsonContext.Default.PolicyAssessmentV1Contract);
        Assert.NotNull(copy);
        Assert.Equal(assessment.InputContentHash, copy.InputContentHash);
        Assert.Equal(assessment.Policy, copy.Policy);
        Assert.Equal(assessment.SupportingFactors[0].Text, copy.SupportingFactors[0].Text);
    }

    [Fact]
    public void PolicyAssessment_UnknownPropertyMissingPropertyAndUnknownEnum_AreRejected()
    {
        var root = JsonNode.Parse(JsonSerializer.Serialize(Assessment(), Section7JsonContext.Default.PolicyAssessmentV1Contract))!.AsObject();
        root["executableQuantity"] = 1;
        AssertInvalid("policy-assessment.schema.json", root.ToJsonString());
        root.Remove("executableQuantity"); root.Remove("inputContentHash");
        AssertInvalid("policy-assessment.schema.json", root.ToJsonString());
        root["inputContentHash"] = JsonSerializer.SerializeToNode(new ContentHash("a")); root["policy"] = "FuturePolicy";
        AssertInvalid("policy-assessment.schema.json", root.ToJsonString());
    }

    [Fact]
    public void OrderIntent_FractionalQuantityAndUnsupportedVersion_AreRejected()
    {
        var json = JsonSerializer.Serialize(new OrderIntentV1(
            "1.0", new IntentId(Guid.Parse("11111111-1111-1111-1111-111111111111")), "intent-1", "client-1",
            "actions-1", "risk-1", new ContractIdentifier("AAPL270115C00200000"), "Sell", 1, "Limit",
            new Money(1.25m, "USD"), "Day", DateTimeOffset.Parse("2026-08-29T12:05:00Z"), ["cancel-after-deadline"],
            "open-short-call", "checkpoint-1"), Section7JsonContext.Default.OrderIntentV1);
        AssertValid("order-intent.schema.json", json);
        var root = JsonNode.Parse(json)!.AsObject(); root["quantity"] = 1.5m;
        AssertInvalid("order-intent.schema.json", root.ToJsonString());
        root["quantity"] = 1; root["schemaVersion"] = "2.0";
        AssertInvalid("order-intent.schema.json", root.ToJsonString());
    }

    private static PolicyAssessmentV1Contract Assessment() => new(
        "1.0", new ContentHash("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"),
        [new("liquidity acceptable", [new EvidenceId("evidence-1")])],
        [new("event risk present", [new EvidenceId("evidence-2")])],
        [], ["dividend date unavailable"], AssessmentPolicy.InsufficientInformation, .42m, "material-missing-data");

    private static void AssertValid(string schemaName, string json) => Assert.True(Evaluate(schemaName, json).IsValid, $"{schemaName} rejected {json}");
    private static void AssertInvalid(string schemaName, string json) => Assert.False(Evaluate(schemaName, json).IsValid);
    private static EvaluationResults Evaluate(string schemaName, string json)
    {
        JsonSchema schema;
        lock (SchemaLock)
        {
            if (!Schemas.TryGetValue(schemaName, out schema!))
            {
                schema = JsonSchema.FromText(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "schemas", schemaName)));
                Schemas.Add(schemaName, schema);
            }
        }
        using var document = JsonDocument.Parse(json);
        return schema.Evaluate(document.RootElement, new EvaluationOptions { OutputFormat = OutputFormat.List });
    }
    private static void AssertStrictObjects(JsonElement value, string path)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            if (value.TryGetProperty("type", out var type) && type.ValueKind == JsonValueKind.String && type.GetString() == "object")
            {
                Assert.True(value.TryGetProperty("additionalProperties", out var additional), $"{path} has an open object schema.");
                Assert.False(additional.GetBoolean(), $"{path} has an open object schema.");
            }
            foreach (var property in value.EnumerateObject()) AssertStrictObjects(property.Value, $"{path}.{property.Name}");
        }
        else if (value.ValueKind == JsonValueKind.Array)
            foreach (var item in value.EnumerateArray()) AssertStrictObjects(item, path);
    }
}
