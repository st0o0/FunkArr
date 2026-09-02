using System.Text.Json.Serialization;

namespace FunkArr.ArrApi.Sabnzbd.Models;

public sealed record FullStatusResponse(
    [property: JsonPropertyName("status")] FullStatusData Status);

public sealed record FullStatusData(
    [property: JsonPropertyName("paused")] bool Paused,
    [property: JsonPropertyName("speedlimit")] string Speedlimit,
    [property: JsonPropertyName("diskspace1")] string Diskspace1,
    [property: JsonPropertyName("diskspace2")] string Diskspace2,
    [property: JsonPropertyName("completedir")] string Completedir,
    [property: JsonPropertyName("speed")] string Speed);
