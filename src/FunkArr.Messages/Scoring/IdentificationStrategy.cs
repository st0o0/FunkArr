using System.Text.Json.Serialization;

namespace FunkArr.Messages.Scoring;

[JsonConverter(typeof(JsonStringEnumConverter<IdentificationStrategy>))]
public enum IdentificationStrategy
{
    [JsonStringEnumMemberName("seasonAndEpisodeNumber")] SeasonAndEpisodeNumber,
    [JsonStringEnumMemberName("byAbsoluteEpisodeNumber")] AbsoluteEpisodeNumber,
    [JsonStringEnumMemberName("itemTitleExact")] TitleExact,
    [JsonStringEnumMemberName("itemTitleIncludes")] TitleIncludes,
    [JsonStringEnumMemberName("itemTitleEqualsAirdate")] AirdateExtraction,
}
