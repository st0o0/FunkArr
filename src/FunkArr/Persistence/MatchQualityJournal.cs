using FunkArr.RuleSet;
using Newtonsoft.Json;

namespace FunkArr.Persistence;

public sealed class MatchRecordedJournal
{
    [JsonProperty("v")] public int Version { get; set; } = 1;
    [JsonProperty("r")] public string RecordJson { get; set; } = "";
}

public sealed class MatchesExpiredJournal
{
    [JsonProperty("v")] public int Version { get; set; } = 1;
    [JsonProperty("ts")] public long OlderThanUtcTicks { get; set; }
}

public static class MatchQualityJournalExtensions
{
    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
    };

    public static MatchRecordedJournal ToJournal(this MatchQualityActor.MatchRecorded evt) => new()
    {
        RecordJson = System.Text.Json.JsonSerializer.Serialize(evt.Record, JsonOptions),
    };

    public static MatchQualityActor.MatchRecorded ToDomain(this MatchRecordedJournal j)
    {
        var record = System.Text.Json.JsonSerializer.Deserialize<MatchRecord>(j.RecordJson, JsonOptions)!;
        return new MatchQualityActor.MatchRecorded(record);
    }

    public static MatchesExpiredJournal ToJournal(this MatchQualityActor.MatchesExpired evt) => new()
    {
        OlderThanUtcTicks = evt.OlderThan.UtcTicks,
    };

    public static MatchQualityActor.MatchesExpired ToDomain(this MatchesExpiredJournal j) =>
        new(new DateTimeOffset(j.OlderThanUtcTicks, TimeSpan.Zero));
}
