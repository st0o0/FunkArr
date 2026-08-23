using System.Text.Json.Serialization;

namespace FunkArr.Api.Models;

public sealed record SabnzbdVersionResponse(
    [property: JsonPropertyName("version")] string Version);

public sealed record SabnzbdConfigResponse(
    [property: JsonPropertyName("config")] SabnzbdConfig Config);

public sealed record SabnzbdConfig(
    [property: JsonPropertyName("misc")] SabnzbdMiscConfig Misc,
    [property: JsonPropertyName("categories")] SabnzbdCategory[] Categories);

public sealed record SabnzbdMiscConfig(
    [property: JsonPropertyName("complete_dir")] string CompleteDir);

public sealed record SabnzbdCategory(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("dir")] string Dir,
    [property: JsonPropertyName("order")] int Order,
    [property: JsonPropertyName("pp")] string Pp);

public sealed record SabnzbdQueueResponse(
    [property: JsonPropertyName("queue")] SabnzbdQueue Queue);

public sealed record SabnzbdQueue(
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("slots")] SabnzbdQueueSlot[] Slots,
    [property: JsonPropertyName("speed")] string Speed,
    [property: JsonPropertyName("timeleft")] string Timeleft,
    [property: JsonPropertyName("mb")] string Mb,
    [property: JsonPropertyName("mbleft")] string Mbleft);

public sealed record SabnzbdQueueSlot(
    [property: JsonPropertyName("nzo_id")] string NzoId,
    [property: JsonPropertyName("filename")] string Filename,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("percentage")] string Percentage,
    [property: JsonPropertyName("mb")] string Mb,
    [property: JsonPropertyName("mbleft")] string Mbleft,
    [property: JsonPropertyName("timeleft")] string Timeleft);

public sealed record SabnzbdHistoryResponse(
    [property: JsonPropertyName("history")] SabnzbdHistory History);

public sealed record SabnzbdHistory(
    [property: JsonPropertyName("slots")] SabnzbdHistorySlot[] Slots,
    [property: JsonPropertyName("noofslots")] int NoofSlots);

public sealed record SabnzbdHistorySlot(
    [property: JsonPropertyName("nzo_id")] string NzoId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("storage")] string Storage,
    [property: JsonPropertyName("completed")] long Completed,
    [property: JsonPropertyName("fail_message")] string FailMessage);

public sealed record SabnzbdAddFileResponse(
    [property: JsonPropertyName("status")] bool Status,
    [property: JsonPropertyName("nzo_ids")] string[]? NzoIds = null,
    [property: JsonPropertyName("error")] string? Error = null);

public sealed record SabnzbdErrorResponse(
    [property: JsonPropertyName("status")] bool Status,
    [property: JsonPropertyName("error")] string Error);
