using System.Text.Json.Serialization;

namespace FunkArr.Messages.Scoring;

[JsonConverter(typeof(JsonStringEnumConverter<FilterField>))]
public enum FilterField
{
    [JsonStringEnumMemberName("title")] Title,
    [JsonStringEnumMemberName("topic")] Topic,
    [JsonStringEnumMemberName("channel")] Channel,
    [JsonStringEnumMemberName("description")] Description,
    [JsonStringEnumMemberName("duration")] Duration,
    [JsonStringEnumMemberName("timestamp")] Timestamp,
}
