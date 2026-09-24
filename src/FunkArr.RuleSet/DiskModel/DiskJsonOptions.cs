using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using FunkArr.Messages;
using FunkArr.Messages.Enrichment;
using FunkArr.Messages.Scoring;

namespace FunkArr.RuleSet.DiskModel;

public static class DiskJsonOptions
{
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new JsonStringEnumConverter<MediaType>(JsonNamingPolicy.CamelCase),
            new JsonStringEnumConverter<FilterField>(JsonNamingPolicy.CamelCase),
            new JsonStringEnumConverter<FilterOp>(JsonNamingPolicy.CamelCase),
            new JsonStringEnumConverter<TitlePartType>(JsonNamingPolicy.CamelCase),
            new JsonStringEnumConverter<EnrichmentMethod>(JsonNamingPolicy.CamelCase),
            new JsonStringEnumConverter<RuntimeMode>(JsonNamingPolicy.CamelCase),
        },
    };

    public static readonly JsonSerializerOptions WriteFormatted = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters =
        {
            new JsonStringEnumConverter<MediaType>(JsonNamingPolicy.CamelCase),
            new JsonStringEnumConverter<FilterField>(JsonNamingPolicy.CamelCase),
            new JsonStringEnumConverter<FilterOp>(JsonNamingPolicy.CamelCase),
            new JsonStringEnumConverter<TitlePartType>(JsonNamingPolicy.CamelCase),
            new JsonStringEnumConverter<EnrichmentMethod>(JsonNamingPolicy.CamelCase),
            new JsonStringEnumConverter<RuntimeMode>(JsonNamingPolicy.CamelCase),
        },
    };
}
