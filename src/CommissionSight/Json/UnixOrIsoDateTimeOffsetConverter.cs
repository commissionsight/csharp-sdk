using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CommissionSight.Json;

/// <summary>
/// The API serializes timestamps as either a Unix epoch in milliseconds (a JSON
/// number) or an ISO-8601 string, depending on the field. This converter accepts
/// both and surfaces a real <see cref="DateTimeOffset"/> to callers.
/// </summary>
public sealed class UnixOrIsoDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Number:
                return DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64());
            case JsonTokenType.String:
                var s = reader.GetString();
                if (long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ms))
                {
                    return DateTimeOffset.FromUnixTimeMilliseconds(ms);
                }

                return DateTimeOffset.Parse(s!, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            default:
                throw new JsonException($"Unexpected token {reader.TokenType} for DateTimeOffset.");
        }
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        // Write the millisecond epoch — the API's primary representation.
        writer.WriteNumberValue(value.ToUnixTimeMilliseconds());
    }
}
