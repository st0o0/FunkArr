using System.Text.Json.Serialization;

namespace FunkArr.MatchMagic;

[JsonConverter(typeof(JsonStringEnumConverter<MatchStrategy>))]
public enum MatchStrategy
{
    [JsonStringEnumMemberName("seasonAndEpisodeNumber")]
    SeasonAndEpisodeNumber,

    [JsonStringEnumMemberName("itemTitleExact")]
    ItemTitleExact,

    [JsonStringEnumMemberName("itemTitleIncludes")]
    ItemTitleIncludes,

    [JsonStringEnumMemberName("itemTitleEqualsAirdate")]
    ItemTitleEqualsAirdate,

    [JsonStringEnumMemberName("byAbsoluteEpisodeNumber")]
    ByAbsoluteEpisodeNumber,
}
