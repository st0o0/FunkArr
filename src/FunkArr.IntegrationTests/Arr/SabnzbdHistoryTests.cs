using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FunkArr.Core;
using FunkArr.Messages.Download;

namespace FunkArr.IntegrationTests.Arr;

[Collection("Sabnzbd")]
public sealed class SabnzbdHistoryTests(FunkArrFixture fixture)
{
    private readonly FunkArrFixture _fixture = fixture;

    [Fact]
    public async Task History_with_items_returns_sabnzbd_format_slots()
    {
        var historyProbe = _fixture.GetProbe<IDownloadHistoryManager>();

        var task = _fixture.Client.GetAsync("/download/api?mode=history&apikey=test-key");

        var msg = historyProbe.ExpectMsg<QueryHistory>();
        var items = new[]
        {
            TestData.CompletedItem(),
            TestData.FailedItem(),
        };
        historyProbe.Reply(new HistoryResult(items, 2));

        var response = await task;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var history = json.GetProperty("history");
        Assert.Equal(2, history.GetProperty("noofslots").GetInt32());

        var slots = history.GetProperty("slots");
        Assert.Equal(2, slots.GetArrayLength());

        var firstSlot = slots[0];
        Assert.True(firstSlot.TryGetProperty("nzo_id", out _));
        Assert.True(firstSlot.TryGetProperty("name", out _));
        Assert.True(firstSlot.TryGetProperty("status", out _));
    }

    [Fact]
    public async Task History_delete_returns_success()
    {
        var historyProbe = _fixture.GetProbe<IDownloadHistoryManager>();
        var downloadId = Guid.NewGuid();

        var task = _fixture.Client.GetAsync(
            $"/download/api?mode=history&name=delete&value={downloadId}&apikey=test-key");

        var msg = historyProbe.ExpectMsg<RemoveHistoryEntry>();
        Assert.Equal(downloadId, msg.DownloadId);
        historyProbe.Reply(new DeleteDownloadResult(true, null));

        var response = await task;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetProperty("status").GetBoolean());
    }
}
