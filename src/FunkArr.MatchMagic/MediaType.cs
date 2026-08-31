using System.Text.Json.Serialization;

namespace FunkArr.MatchMagic;

[JsonConverter(typeof(JsonStringEnumConverter<MediaType>))]
public enum MediaType
{
    [JsonStringEnumMemberName("show")]
    Show,

    [JsonStringEnumMemberName("movie")]
    Movie,
}
