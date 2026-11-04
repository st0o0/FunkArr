using System.Text.Json.Serialization;

namespace FunkArr.ArrApi.Sabnzbd.Models;

[JsonConverter(typeof(JsonStringEnumConverter<SabnzbdQueueStatus>))]
public enum SabnzbdQueueStatus
{
    Unknown = 0,
    [JsonStringEnumMemberName("Downloading")] Downloading = 1,
    [JsonStringEnumMemberName("Queued")] Queued = 2
}

[JsonConverter(typeof(JsonStringEnumConverter<SabnzbdHistoryStatus>))]
public enum SabnzbdHistoryStatus
{
    Unknown = 0,
    [JsonStringEnumMemberName("Completed")] Completed = 1,
    [JsonStringEnumMemberName("Failed")] Failed = 2
}

[JsonConverter(typeof(JsonStringEnumConverter<SabnzbdPriority>))]
public enum SabnzbdPriority
{
    Unknown = 0,
    [JsonStringEnumMemberName("Low")] Low = 1,
    [JsonStringEnumMemberName("Normal")] Normal = 2,
    [JsonStringEnumMemberName("High")] High = 3
}
