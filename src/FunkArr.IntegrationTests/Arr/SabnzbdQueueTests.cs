using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FunkArr.Core;
using FunkArr.Messages.Download;

namespace FunkArr.IntegrationTests.Arr;

[Collection("Sabnzbd")]
public sealed class SabnzbdQueueTests(FunkArrFixture fixture)
{
    private readonly FunkArrFixture _fixture = fixture;

    [Fact]
    public async Task Queue_with_items_returns_sabnzbd_format_slots()
    {
        var downloadProbe = _fixture.GetProbe<IDownloadManager>();

        var task = _fixture.Client.GetAsync("/download/api?mode=queue&apikey=test-key");

        var msg = downloadProbe.ExpectMsg<QueryQueue>();
        var items = new[]
        {
            TestData.QueueItem("Tatort.S01E05.720p.WEB-DL"),
            TestData.ProcessingItem(),
        };
        downloadProbe.Reply(new QueueResult(items, 2, 2, false, false, null));

        var response = await task;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var queue = json.GetProperty("queue");
        var slots = queue.GetProperty("slots");
        Assert.Equal(2, slots.GetArrayLength());

        var firstSlot = slots[0];
        Assert.True(firstSlot.TryGetProperty("nzo_id", out _));
        Assert.True(firstSlot.TryGetProperty("filename", out _));
        Assert.True(firstSlot.TryGetProperty("status", out _));
    }

    [Fact]
    public async Task Queue_delete_returns_success()
    {
        var downloadProbe = _fixture.GetProbe<IDownloadManager>();
        var downloadId = Guid.NewGuid();

        var task = _fixture.Client.GetAsync(
            $"/download/api?mode=queue&name=delete&value={downloadId}&apikey=test-key");

        var msg = downloadProbe.ExpectMsg<DeleteDownload>();
        Assert.Equal(downloadId, msg.DownloadId);
        downloadProbe.Reply(new DeleteDownloadResult(true, null));

        var response = await task;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetProperty("status").GetBoolean());
    }

    [Fact]
    public async Task Queue_priority_returns_success()
    {
        var downloadProbe = _fixture.GetProbe<IDownloadManager>();
        var downloadId = Guid.NewGuid();

        var task = _fixture.Client.GetAsync(
            $"/download/api?mode=queue&name=priority&value={downloadId}&value2=1&apikey=test-key");

        var msg = downloadProbe.ExpectMsg<SetDownloadPriority>();
        Assert.Equal(downloadId, msg.DownloadId);
        downloadProbe.Reply(new SetDownloadPriorityCompleted());

        var response = await task;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetProperty("status").GetBoolean());
    }

    [Fact]
    public async Task Queue_swap_returns_success()
    {
        var downloadProbe = _fixture.GetProbe<IDownloadManager>();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        var task = _fixture.Client.GetAsync(
            $"/download/api?mode=queue&name=switch&value={id1}&value2={id2}&apikey=test-key");

        var msg = downloadProbe.ExpectMsg<SwapDownloads>();
        Assert.Equal(id1, msg.DownloadId1);
        Assert.Equal(id2, msg.DownloadId2);
        downloadProbe.Reply(new SwapDownloadsCompleted());

        var response = await task;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetProperty("status").GetBoolean());
    }
}
