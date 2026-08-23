using FunkArr.RuleSet;
using Newtonsoft.Json;

namespace FunkArr.Persistence;

public sealed class MatchRecordedDto
{
    [JsonProperty("v")] public int Version { get; set; } = 1;
    [JsonProperty("r")] public string RecordJson { get; set; } = "";
}

public sealed class MatchesExpiredDto
{
    [JsonProperty("v")] public int Version { get; set; } = 1;
    [JsonProperty("ts")] public long OlderThanUtcTicks { get; set; }
}

public static class MatchQualityEventDtoMapping
{
    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
    };

    public static MatchRecordedDto ToDto(MatchQualityWorker.MatchRecorded evt) => new()
    {
        RecordJson = System.Text.Json.JsonSerializer.Serialize(evt.Record, JsonOptions),
    };

    public static MatchQualityWorker.MatchRecorded ToDomain(MatchRecordedDto dto)
    {
        var record = System.Text.Json.JsonSerializer.Deserialize<MatchRecord>(dto.RecordJson, JsonOptions)!;
        return new MatchQualityWorker.MatchRecorded(record);
    }

    public static MatchesExpiredDto ToDto(MatchQualityWorker.MatchesExpired evt) => new()
    {
        OlderThanUtcTicks = evt.OlderThan.UtcTicks,
    };

    public static MatchQualityWorker.MatchesExpired ToDomain(MatchesExpiredDto dto) =>
        new(new DateTimeOffset(dto.OlderThanUtcTicks, TimeSpan.Zero));
}
