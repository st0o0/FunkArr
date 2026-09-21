using System.Text.Json.Serialization;

namespace FunkArr.Messages;

public enum MediaType
{
    [JsonStringEnumMemberName("show")] Show,
    [JsonStringEnumMemberName("movie")] Movie,
}
