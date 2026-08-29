using System.Reflection;
using System.Text.Json;
using AlpacaAgent.Application.Workflow;
using Json.Schema;
using Xunit;

namespace AlpacaAgent.UnitTests.Acceptance;

public sealed class AcceptanceCatalogIntegrityTests
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public void AcceptanceCatalog_SpecificationManifestAndDiscovery_AreComplete()
    {
        var basePath = AppContext.BaseDirectory;
        var manifestText = File.ReadAllText(Path.Combine(basePath, "acceptance-manifest.json"));
        var schema = JsonSchema.FromText(File.ReadAllText(Path.Combine(basePath, "acceptance-manifest.schema.json")));
        using var manifestDocument = JsonDocument.Parse(manifestText);
        var evaluation = schema.Evaluate(manifestDocument.RootElement, new EvaluationOptions { OutputFormat = OutputFormat.List });
        Assert.True(evaluation.IsValid, evaluation.ToString());

        var entries = JsonSerializer.Deserialize<AcceptanceManifestEntry[]>(manifestText, JsonOptions)!;
        Assert.Equal(180, entries.Length);
        Assert.Equal(180, entries.Select(entry => entry.StableId).Distinct(StringComparer.Ordinal).Count());
        Assert.All(entries, entry =>
        {
            Assert.NotNull(entry.FixtureIds);
            Assert.False(string.IsNullOrWhiteSpace(entry.OwningPhase));
            Assert.False(string.IsNullOrWhiteSpace(entry.ProductionComponent));
        });

        var discovered = typeof(AcceptanceCatalogIntegrityTests).Assembly.GetTypes()
            .SelectMany(type => type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            .SelectMany(method => method.CustomAttributes
                .Where(attribute => attribute.AttributeType == typeof(TraitAttribute)
                    && (string?)attribute.ConstructorArguments[0].Value == "StableId")
                .Select(attribute => new { Method = method, Id = (string)attribute.ConstructorArguments[1].Value! }))
            .ToArray();

        var manifest = entries.ToDictionary(entry => entry.StableId, StringComparer.Ordinal);
        var applicationAssembly = typeof(DurableWorkflowRunner).Assembly;
        Assert.Null(applicationAssembly.GetType("AlpacaAgent.Application.Acceptance.SpecificationAcceptanceSkeleton"));
        Assert.Null(applicationAssembly.GetType("AlpacaAgent.Application.Acceptance.IAcceptanceContract"));
        Assert.Equal(entries.Select(entry => entry.StableId).Order(StringComparer.Ordinal), discovered.Select(item => item.Id).Order(StringComparer.Ordinal));
        Assert.All(discovered, item =>
        {
            Assert.NotNull(item.Method.GetCustomAttribute<FactAttribute>());
            Assert.Equal(manifest[item.Id].Method, item.Method.Name);
            Assert.Null(item.Method.GetCustomAttribute<TheoryAttribute>());
            Assert.DoesNotContain(item.Method.CustomAttributes, attribute =>
                attribute.AttributeType.Name.Contains("Skip", StringComparison.OrdinalIgnoreCase)
                || attribute.AttributeType.Name.Contains("Explicit", StringComparison.OrdinalIgnoreCase));
            var gateStatus = item.Method.CustomAttributes.Single(attribute => attribute.AttributeType == typeof(TraitAttribute)
                && (string?)attribute.ConstructorArguments[0].Value == "GateStatus");
            Assert.Equal(manifest[item.Id].ImplementationStatus == "IMPLEMENTED" ? "ActiveThroughPhase3" : "ExpectedRedFuture",
                (string?)gateStatus.ConstructorArguments[1].Value);
            if (manifest[item.Id].ImplementationStatus == "IMPLEMENTED")
                Assert.False(typeof(AcceptanceTestBase).IsAssignableFrom(item.Method.DeclaringType));
            else
            {
                var boundary = applicationAssembly.GetType($"AlpacaAgent.Application.Acceptance.{manifest[item.Id].ProductionComponent}FutureBoundary");
                Assert.NotNull(boundary);
                Assert.NotNull(boundary.GetMethod(item.Method.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly));
            }
        });
    }
}
