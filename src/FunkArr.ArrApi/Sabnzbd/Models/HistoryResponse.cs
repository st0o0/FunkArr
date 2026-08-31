using System.Text.Json.Serialization;

namespace FunkArr.ArrApi.Sabnzbd.Models;

public sealed record HistoryResponse(
    [property: JsonPropertyName("history")] HistoryData History);

public sealed record HistoryData(
    [property: JsonPropertyName("noofslots")] int NoofSlots,
    [property: JsonPropertyName("slots")] IReadOnlyList<HistorySlot> Slots);

public sealed record HistorySlot(
    [property: JsonPropertyName("nzo_id")] string NzoId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("nzb_name")] string NzbName,
    [property: JsonPropertyName("category")] string Category,
    [property: JsonPropertyName("bytes")] long Bytes,
    [property: JsonPropertyName("download_time")] int DownloadTime,
    [property: JsonPropertyName("storage")] string? Storage,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("fail_message")] string FailMessage,
    [property: JsonPropertyName("completed_on")] long CompletedOn);
