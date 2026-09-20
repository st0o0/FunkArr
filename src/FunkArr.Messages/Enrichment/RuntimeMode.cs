using System.Text.Json.Serialization;

namespace FunkArr.Messages.Enrichment;

[JsonConverter(typeof(JsonStringEnumConverter<RuntimeMode>))]
public enum RuntimeMode
{
    [JsonStringEnumMemberName("tiebreaker")] Tiebreaker,
    [JsonStringEnumMemberName("filter")] Filter,
}
