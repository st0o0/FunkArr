using System.Text.Json;
using System.Text.Json.Serialization;

namespace FunkArr.Search;

internal sealed record MediathekApiResponse(
    [property: JsonPropertyName("result")] MediathekApiResult? Result,
    [property: JsonPropertyName("err")] string? Err);

internal sealed record MediathekApiResult(
    [property: JsonPropertyName("results")] MediathekApiItem[]? Results,
    [property: JsonPropertyName("queryInfo")] MediathekApiQueryInfo? QueryInfo);

internal sealed record MediathekApiQueryInfo(
    [property: JsonPropertyName("totalResults")] int TotalResults);

internal sealed record MediathekApiItem(
    [property: JsonPropertyName("channel")] string? Channel,
    [property: JsonPropertyName("topic")] string? Topic,
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("timestamp")] long Timestamp,
    [property: JsonPropertyName("duration")] int Duration,
    [property: JsonPropertyName("size")] long? Size,
    [property: JsonPropertyName("url_video")] string? UrlVideo,
    [property: JsonPropertyName("url_video_low")] string? UrlVideoLow,
    [property: JsonPropertyName("url_video_hd")] string? UrlVideoHd,
    [property: JsonPropertyName("url_subtitle")] string? UrlSubtitle,
    [property: JsonPropertyName("url_website")] string? UrlWebsite);

internal sealed class EmptyStringToNullConverter : JsonConverter<string?>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.String ? reader.GetString() is { Length: > 0 } s ? s : null : null;

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        if (value is null)
            writer.WriteNullValue();
        else
            writer.WriteStringValue(value);
    }
}
