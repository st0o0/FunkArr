using System.Text.Json;
using FunkArr.ArrApi.Sabnzbd.Models;
using Xunit;

namespace FunkArr.ArrApi.Tests.Sabnzbd;

public sealed class SabnzbdJsonTests
{
    [Fact]
    public void QueueResponse_serializes_with_correct_property_names()
    {
        var response = new QueueResponse(new QueueData(false, "", 0, "0", "0", "0", []));
        var json = JsonSerializer.Serialize(response);

        Assert.Contains("\"queue\"", json);
        Assert.Contains("\"slots\"", json);
        Assert.Contains("\"paused\":false", json);
        Assert.Contains("\"noofslots_total\":0", json);
        Assert.Contains("\"speed\":\"0\"", json);
    }

    [Fact]
    public void QueueSlot_serializes_with_sab_property_names()
    {
        var slot = new QueueSlot("abc123", "Downloading", 0, "01:00:00", "500", "test.mkv", "sonarr", "250", "50", "Normal");
        var json = JsonSerializer.Serialize(slot);

        Assert.Contains("\"nzo_id\":\"abc123\"", json);
        Assert.Contains("\"status\":\"Downloading\"", json);
        Assert.Contains("\"filename\":\"test.mkv\"", json);
        Assert.Contains("\"cat\":\"sonarr\"", json);
        Assert.Contains("\"mbleft\":\"250\"", json);
        Assert.Contains("\"percentage\":\"50\"", json);
        Assert.Contains("\"priority\":\"Normal\"", json);
    }

    [Fact]
    public void HistoryResponse_serializes_with_correct_property_names()
    {
        var response = new HistoryResponse(new HistoryData(0, []));
        var json = JsonSerializer.Serialize(response);

        Assert.Contains("\"history\"", json);
        Assert.Contains("\"slots\"", json);
        Assert.Contains("\"noofslots\":0", json);
    }

    [Fact]
    public void HistorySlot_serializes_with_sab_property_names()
    {
        var slot = new HistorySlot("abc123", "test.mkv", "test.mkv", "sonarr", 1500000000, 120, "/downloads/test.mkv", "Completed", "", 1700000000);
        var json = JsonSerializer.Serialize(slot);

        Assert.Contains("\"nzo_id\":\"abc123\"", json);
        Assert.Contains("\"nzb_name\":\"test.mkv\"", json);
        Assert.Contains("\"download_time\":120", json);
        Assert.Contains("\"storage\":\"/downloads/test.mkv\"", json);
        Assert.Contains("\"status\":\"Completed\"", json);
        Assert.Contains("\"fail_message\":\"\"", json);
        Assert.Contains("\"completed_on\":1700000000", json);
    }

    [Fact]
    public void HistorySlot_null_storage_serializes()
    {
        var slot = new HistorySlot("abc", "test", "test", "tv", 0, 0, null, "Failed", "connection error", 0);
        var json = JsonSerializer.Serialize(slot);

        Assert.Contains("\"storage\":null", json);
        Assert.Contains("\"fail_message\":\"connection error\"", json);
    }

    [Fact]
    public void FullStatusResponse_serializes_correctly()
    {
        var response = new FullStatusResponse(new FullStatusData(false, "", "100", "100", "/downloads"));
        var json = JsonSerializer.Serialize(response);

        Assert.Contains("\"status\"", json);
        Assert.Contains("\"paused\":false", json);
        Assert.Contains("\"diskspace1\":\"100\"", json);
        Assert.Contains("\"completedir\":\"/downloads\"", json);
    }
}
