using System.Text.Json.Serialization;

namespace FunkArr.ArrApi.Sabnzbd.Models;

public sealed record QueueResponse(
    [property: JsonPropertyName("queue")] QueueData Queue);

public sealed record QueueData(
    [property: JsonPropertyName("paused")] bool Paused,
    [property: JsonPropertyName("speedlimit")] string Speedlimit,
    [property: JsonPropertyName("noofslots_total")] int NoofSlotsTotal,
    [property: JsonPropertyName("diskspace1")] string Diskspace1,
    [property: JsonPropertyName("diskspace2")] string Diskspace2,
    [property: JsonPropertyName("speed")] string Speed,
    [property: JsonPropertyName("slots")] IReadOnlyList<QueueSlot> Slots);

public sealed record QueueSlot(
    [property: JsonPropertyName("nzo_id")] string NzoId,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("index")] int Index,
    [property: JsonPropertyName("timeleft")] string Timeleft,
    [property: JsonPropertyName("mb")] string Mb,
    [property: JsonPropertyName("filename")] string Filename,
    [property: JsonPropertyName("cat")] string Cat,
    [property: JsonPropertyName("mbleft")] string Mbleft,
    [property: JsonPropertyName("percentage")] string Percentage,
    [property: JsonPropertyName("priority")] string Priority,
    [property: JsonPropertyName("speed")] string Speed);
