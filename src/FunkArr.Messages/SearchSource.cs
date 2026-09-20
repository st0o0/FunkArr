using System.Text.Json.Serialization;

namespace FunkArr.Messages;

[JsonConverter(typeof(JsonStringEnumConverter<SearchSource>))]
public enum SearchSource
{
    [JsonStringEnumMemberName("sonarr")] Sonarr,
    [JsonStringEnumMemberName("radarr")] Radarr,
    [JsonStringEnumMemberName("prowlarr")] Prowlarr,
    [JsonStringEnumMemberName("ui")] Ui,
    [JsonStringEnumMemberName("test")] Test,
}
