using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Json.Schema;

namespace AlpacaAgent.ContractTests;

public sealed class FixtureCatalogTests
{
    [Fact]
    public void RequiredScenarioFixtures_ValidateAndHaveCanonicalPayloadHashes()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "fixtures");
        var json = File.ReadAllText(Path.Combine(root, "fixture-catalog.json"));
        var schema = JsonSchema.FromText(File.ReadAllText(Path.Combine(root, "fixture-catalog.schema.json")));
        using var document = JsonDocument.Parse(json);
        Assert.True(schema.Evaluate(document.RootElement).IsValid);
        var entries = document.RootElement.EnumerateArray().ToArray();
        Assert.Equal(17, entries.Length);
        Assert.Equal(17, entries.Select(entry => entry.GetProperty("fixtureId").GetString()).Distinct(StringComparer.Ordinal).Count());
        foreach (var entry in entries)
        {
            var payload = JsonSerializer.Serialize(entry.GetProperty("payload"));
            var actual = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
            Assert.Equal(entry.GetProperty("canonicalContentHash").GetString(), actual);
            Assert.True(entry.GetProperty("immutable").GetBoolean());
        }
    }
}
