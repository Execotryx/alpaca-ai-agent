using System.Reflection;
using System.Text.Json;
using Xunit;

namespace AlpacaAgent.UnitTests.Acceptance;

public sealed class AcceptanceCatalogIntegrityTests
{
    [Fact]
    public void AcceptanceCatalog_SpecificationManifestAndDiscovery_AreComplete()
    {
        var manifestPath = Path.Combine(AppContext.BaseDirectory, "acceptance-manifest.json");
        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var manifest = document.RootElement.EnumerateArray()
            .ToDictionary(
                item => item.GetProperty("stableId").GetString()!,
                item => item.GetProperty("method").GetString()!,
                StringComparer.Ordinal);
        var manifestIds = manifest.Keys.Order(StringComparer.Ordinal).ToArray();

        var discovered = typeof(AcceptanceTestBase).Assembly.GetTypes()
            .SelectMany(type => type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            .SelectMany(method => method.CustomAttributes
                .Where(attribute => attribute.AttributeType == typeof(TraitAttribute)
                    && (string?)attribute.ConstructorArguments[0].Value == "StableId")
                .Select(attribute => new
                {
                    Method = method,
                    Id = (string)attribute.ConstructorArguments[1].Value!
                }))
            .ToArray();

        Assert.Equal(180, manifestIds.Length);
        Assert.Equal(180, manifestIds.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(manifestIds, discovered.Select(item => item.Id).Order(StringComparer.Ordinal));
        Assert.All(discovered, item => Assert.NotNull(item.Method.GetCustomAttribute<FactAttribute>()));
        Assert.All(discovered, item => Assert.Equal(manifest[item.Id], item.Method.Name));
        Assert.All(discovered, item => Assert.Matches("^[A-Za-z0-9]+_[A-Za-z0-9]+", item.Method.Name));
    }
}
