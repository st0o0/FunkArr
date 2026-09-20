using System.Text.Json.Serialization;

namespace FunkArr.Messages.Enrichment;

[JsonConverter(typeof(JsonStringEnumConverter<EnrichmentMethod>))]
public enum EnrichmentMethod
{
    [JsonStringEnumMemberName("title")] Title,
    [JsonStringEnumMemberName("airdate")] Airdate,
}
