using System.Text.Json;
using System.Text.Json.Serialization;
using FunkArr.Messages;

namespace FunkArr.ArrApi.Newznab.Models;

public sealed record NzbPayload(
    string Title,
    string Url,
    string? SubtitleUrl,
    string Channel,
    int Duration,
    long Size,
    MediaType Category)
{
    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) }
    };

    public string Encode() => Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(this, _json));

    public static NzbPayload? Decode(string id)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<NzbPayload>(Convert.FromBase64String(id), _json);
            if (payload is not { Title: not null, Url.Length: > 0, Channel: not null })
            {
                return null;
            }

            if (!UrlValidation.HasAllowedScheme(payload.Url))
            {
                return null;
            }

            return payload with
            {
                SubtitleUrl = UrlValidation.HasAllowedScheme(payload.SubtitleUrl) ? payload.SubtitleUrl : null
            };
        }
        catch (Exception ex) when (ex is FormatException or JsonException)
        {
            return null;
        }
    }
}
