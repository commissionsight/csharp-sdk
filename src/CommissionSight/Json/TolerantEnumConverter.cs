using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CommissionSight.Json;

/// <summary>
/// Serializes enums to/from their wire string form, honoring
/// <see cref="JsonStringEnumMemberNameAttribute"/> for custom names. Unrecognized
/// strings deserialize to the enum's <c>Unknown</c> member (value 0) when present,
/// rather than throwing — so a new server-side value never breaks an older SDK.
/// </summary>
public sealed class TolerantEnumConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) => typeToConvert.IsEnum;

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var converterType = typeof(TolerantEnumConverter<>).MakeGenericType(typeToConvert);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}

/// <summary>Per-enum tolerant string converter. Name maps are built once and cached.</summary>
public sealed class TolerantEnumConverter<T> : JsonConverter<T> where T : struct, Enum
{
    private static readonly Dictionary<string, T> NameToValue = BuildNameToValue();
    private static readonly Dictionary<T, string> ValueToName = BuildValueToName();
    private static readonly T Fallback = Enum.TryParse<T>("Unknown", out var u) ? u : default;

    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return Fallback;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var s = reader.GetString();
            if (s is not null && NameToValue.TryGetValue(s, out var value))
            {
                return value;
            }

            return Fallback;
        }

        // Be liberal: accept a raw numeric enum value too.
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt64(out var n))
        {
            return (T)Enum.ToObject(typeof(T), n);
        }

        return Fallback;
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(ValueToName.TryGetValue(value, out var name) ? name : value.ToString());
    }

    private static Dictionary<string, T> BuildNameToValue()
    {
        var map = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, value) in EnumerateMembers())
        {
            map[name] = value;
        }

        return map;
    }

    private static Dictionary<T, string> BuildValueToName()
    {
        var map = new Dictionary<T, string>();
        foreach (var (name, value) in EnumerateMembers())
        {
            map.TryAdd(value, name);
        }

        return map;
    }

    private static IEnumerable<(string Name, T Value)> EnumerateMembers()
    {
        foreach (var field in typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            var attr = field.GetCustomAttribute<JsonStringEnumMemberNameAttribute>();
            var name = attr?.Name ?? field.Name;
            yield return (name, (T)field.GetValue(null)!);
        }
    }
}
