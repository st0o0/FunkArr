using System.Text.Json.Serialization;

namespace FunkArr.ArrApi.Sabnzbd.Models;

public sealed record SabnzbdVersionResponse(
    [property: JsonPropertyName("version")] string Version);

public sealed record SabnzbdErrorResponse(
    [property: JsonPropertyName("status")] bool Status,
    [property: JsonPropertyName("error")] string Error);
