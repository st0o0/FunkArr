using System.Text.Json.Serialization;

namespace FunkArr.ArrApi.Sabnzbd.Models;

public sealed record SabnzbdVersionResponse(
    [property: JsonPropertyName("version")] string Version);

public sealed record SabnzbdErrorResponse(
    [property: JsonPropertyName("status")] bool Status,
    [property: JsonPropertyName("error")] string Error);

public sealed record SabnzbdStatusResponse(
    [property: JsonPropertyName("status")] bool Status);

public sealed record SabnzbdAddResponse(
    [property: JsonPropertyName("status")] bool Status,
    [property: JsonPropertyName("nzo_ids")] IReadOnlyList<string> NzoIds);
