using System.Text.Json.Serialization;

namespace FunkArr.Messages;

[JsonConverter(typeof(JsonStringEnumConverter<MediaType>))]
public enum MediaType
{
    [JsonStringEnumMemberName("show")] Show,
    [JsonStringEnumMemberName("movie")] Movie,
}
