using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AlpacaAgent.Domain.Contracts;

public readonly record struct OptionQuantity
{
    public int Value { get; }
    private OptionQuantity(int value) => Value = value;
    public static OptionQuantity Create(decimal value)
    {
        if (value <= 0 || value != decimal.Truncate(value)) throw new ArgumentOutOfRangeException(nameof(value), "Option quantity must be a positive whole number.");
        return new(checked((int)value));
    }
}

public static class Coverage
{
    public static int CompleteStandardBlocks(decimal verifiedFreeShares) => verifiedFreeShares <= 0 ? 0 : checked((int)decimal.Floor(verifiedFreeShares / 100m));
}

[JsonConverter(typeof(ObservedValueJsonConverterFactory))]
public abstract record ObservedValue<T>
{
    private ObservedValue() { }
    public sealed record Known(T Value) : ObservedValue<T>;
    public sealed record Unknown : ObservedValue<T>;
    public sealed record Unavailable : ObservedValue<T>;
    public sealed record Invalid(string Reason) : ObservedValue<T>;
}

public sealed class ObservedValueJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) => typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(ObservedValue<>);
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
        (JsonConverter)Activator.CreateInstance(typeof(ObservedValueJsonConverter<>).MakeGenericType(typeToConvert.GetGenericArguments()[0]))!;
}

public sealed class ObservedValueJsonConverter<T> : JsonConverter<ObservedValue<T>>
{
    public override ObservedValue<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;
        var state = root.GetProperty("state").GetString();
        return state switch
        {
            "known" => new ObservedValue<T>.Known(root.GetProperty("value").Deserialize<T>(options)!),
            "unknown" => new ObservedValue<T>.Unknown(),
            "unavailable" => new ObservedValue<T>.Unavailable(),
            "invalid" => new ObservedValue<T>.Invalid(root.GetProperty("reason").GetString()!),
            _ => throw new JsonException($"Unknown observed-value state '{state}'.")
        };
    }
    public override void Write(Utf8JsonWriter writer, ObservedValue<T> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        switch (value)
        {
            case ObservedValue<T>.Known known: writer.WriteString("state", "known"); writer.WritePropertyName("value"); JsonSerializer.Serialize(writer, known.Value, options); break;
            case ObservedValue<T>.Unknown: writer.WriteString("state", "unknown"); break;
            case ObservedValue<T>.Unavailable: writer.WriteString("state", "unavailable"); break;
            case ObservedValue<T>.Invalid invalid: writer.WriteString("state", "invalid"); writer.WriteString("reason", invalid.Reason); break;
            default: throw new JsonException("Unsupported observed-value state.");
        }
        writer.WriteEndObject();
    }
}

public static class MoneyMath
{
    public static decimal Round(decimal value, int decimals, MidpointRounding rule) => decimal.Round(value, decimals, rule);
}

public static class RatioMath
{
    public static ObservedValue<decimal> Divide(decimal numerator, decimal denominator) => denominator == 0m
        ? new ObservedValue<decimal>.Invalid("DIVIDE_BY_ZERO")
        : new ObservedValue<decimal>.Known(numerator / denominator);
}

public static class CanonicalHasher
{
    public static string Sha256(JsonElement value)
    {
        var builder = new StringBuilder();
        WriteCanonical(value, builder);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()))).ToLowerInvariant();
    }
    private static void WriteCanonical(JsonElement value, StringBuilder builder)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                builder.Append('{');
                var first = true;
                foreach (var property in value.EnumerateObject().OrderBy(x => x.Name, StringComparer.Ordinal))
                {
                    if (!first) builder.Append(','); first = false;
                    builder.Append(JsonSerializer.Serialize(property.Name)).Append(':'); WriteCanonical(property.Value, builder);
                }
                builder.Append('}'); break;
            case JsonValueKind.Array:
                builder.Append('['); first = true;
                foreach (var item in value.EnumerateArray()) { if (!first) builder.Append(','); first = false; WriteCanonical(item, builder); }
                builder.Append(']'); break;
            case JsonValueKind.Number:
                builder.Append(value.GetDecimal().ToString("G29", CultureInfo.InvariantCulture)); break;
            default: builder.Append(value.GetRawText()); break;
        }
    }
}

public readonly record struct UtcInstant(DateTimeOffset Value)
{
    public static UtcInstant Parse(string value) => new(DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind).ToUniversalTime());
}

public sealed class ContractVersionRegistry
{
    private readonly Dictionary<(string Contract, int From), Func<JsonElement, JsonElement>> migrations = [];
    public void Register(string contract, int fromVersion, Func<JsonElement, JsonElement> migration) => migrations.Add((contract, fromVersion), migration);
    public JsonElement Migrate(string contract, int fromVersion, int currentVersion, JsonElement payload)
    {
        var result = payload.Clone();
        for (var version = fromVersion; version < currentVersion; version++)
            result = migrations.TryGetValue((contract, version), out var migrate) ? migrate(result) : throw new NotSupportedException($"Unsupported {contract} version {version}.");
        return result;
    }
}

public sealed record ContractShape(IReadOnlySet<string> Required, IReadOnlyDictionary<string, IReadOnlySet<string>> Enums);
public static class StrictContractValidator
{
    public static IReadOnlyList<string> Validate(JsonElement payload, ContractShape shape)
    {
        if (payload.ValueKind != JsonValueKind.Object) return ["ROOT_NOT_OBJECT"];
        var failures = new List<string>();
        var properties = payload.EnumerateObject().ToArray();
        foreach (var property in properties.Where(x => !shape.Required.Contains(x.Name))) failures.Add($"UNKNOWN_PROPERTY:{property.Name}");
        foreach (var required in shape.Required.Where(x => !payload.TryGetProperty(x, out _))) failures.Add($"MISSING_REQUIRED:{required}");
        foreach (var pair in shape.Enums)
            if (payload.TryGetProperty(pair.Key, out var value) && (value.ValueKind != JsonValueKind.String || !pair.Value.Contains(value.GetString()!))) failures.Add($"UNKNOWN_ENUM:{pair.Key}");
        return failures.Order(StringComparer.Ordinal).ToArray();
    }
}
