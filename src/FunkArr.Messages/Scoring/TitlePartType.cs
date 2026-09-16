using System.Text.Json.Serialization;

namespace FunkArr.Messages.Scoring;

[JsonConverter(typeof(JsonStringEnumConverter<TitlePartType>))]
public enum TitlePartType
{
    [JsonStringEnumMemberName("static")] Static,
    [JsonStringEnumMemberName("regex")] Regex,
}
