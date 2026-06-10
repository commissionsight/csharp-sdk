using System.Text.Json;
using System.Text.Json.Serialization;

namespace CommissionSight.Json;

/// <summary>Shared, immutable JSON options used for every request and response.</summary>
public static class CommissionSightJson
{
    /// <summary>
    /// camelCase property naming (matching the API), case-insensitive reads,
    /// tolerant string enums, flexible timestamps, and omission of null members
    /// on write (so optional request fields are simply absent).
    /// </summary>
    public static JsonSerializerOptions Options { get; } = Create(ignoreNulls: true);

    /// <summary>
    /// Like <see cref="Options"/> but writes <c>null</c> members instead of omitting
    /// them — used for the few request bodies where an explicit null is meaningful
    /// (e.g. clearing an account's custom billing rate).
    /// </summary>
    public static JsonSerializerOptions OptionsIncludingNulls { get; } = Create(ignoreNulls: false);

    private static JsonSerializerOptions Create(bool ignoreNulls)
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = ignoreNulls ? JsonIgnoreCondition.WhenWritingNull : JsonIgnoreCondition.Never,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
        };
        options.Converters.Add(new TolerantEnumConverterFactory());
        options.Converters.Add(new UnixOrIsoDateTimeOffsetConverter());
        return options;
    }
}
