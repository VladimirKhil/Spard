using System.Text.Json;
using System.Text.Json.Serialization;

namespace Spard.Service.Client;

/// <summary>
/// Allows to serialize and deserialize in JSON <see cref="TimeSpan" /> values.
/// </summary>
internal sealed class TimeSpanConverter : JsonConverter<TimeSpan>
{
    public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return value == null ? TimeSpan.Zero : TimeSpan.Parse(value);
    }

    public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString());
}
