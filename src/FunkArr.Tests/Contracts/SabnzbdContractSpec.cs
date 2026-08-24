using System.Text.Json;
using FunkArr.Api.Contracts.Sabnzbd;

namespace FunkArr.Tests.Contracts;

public sealed class SabnzbdContractSpec
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
    };

    [Fact(Timeout = 5000)]
    public Task VersionResponse_WireFormat()
    {
        var response = new SabnzbdVersionResponse("4.3.3");
        var json = JsonSerializer.Serialize(response, Options);
        return Verify(json);
    }

    [Fact(Timeout = 5000)]
    public Task ConfigResponse_WireFormat()
    {
        var response = new SabnzbdConfigResponse(
            new SabnzbdConfig(
                new SabnzbdMiscConfig("/media/complete"),
                [new SabnzbdCategory("*", "", 0, "")]));
        var json = JsonSerializer.Serialize(response, Options);
        return Verify(json);
    }

    [Fact(Timeout = 5000)]
    public Task QueueResponse_WireFormat()
    {
        var response = new SabnzbdQueueResponse(
            new SabnzbdQueue("Downloading",
                [
                    new SabnzbdQueueSlot("nzo_abc", "Show.S01E01.mkv", "Downloading", "tv", "45", "500", "275", "00:05:30")
                ],
                "10.5", "00:05:30", "500", "275"));
        var json = JsonSerializer.Serialize(response, Options);
        return Verify(json);
    }

    [Fact(Timeout = 5000)]
    public Task HistoryResponse_WireFormat()
    {
        var response = new SabnzbdHistoryResponse(
            new SabnzbdHistory(
                [
                    new SabnzbdHistorySlot("nzo_xyz", "Show.S01E02.mkv", "Completed", "movies", "/media/complete/show.mkv", 1735689600, "")
                ],
                1));
        var json = JsonSerializer.Serialize(response, Options);
        return Verify(json);
    }

    [Fact(Timeout = 5000)]
    public Task AddFileResponse_Success_WireFormat()
    {
        var response = new SabnzbdAddFileResponse(true, ["nzo_abc123"]);
        var json = JsonSerializer.Serialize(response, Options);
        return Verify(json);
    }

    [Fact(Timeout = 5000)]
    public Task ErrorResponse_WireFormat()
    {
        var response = new SabnzbdErrorResponse(false, "Something went wrong");
        var json = JsonSerializer.Serialize(response, Options);
        return Verify(json);
    }
}
