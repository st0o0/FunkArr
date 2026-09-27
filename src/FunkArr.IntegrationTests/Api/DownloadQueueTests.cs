using System.Net;
using System.Net.Http.Json;
using FunkArr.Api.Models;
using FunkArr.Core;
using FunkArr.Messages.Download;

namespace FunkArr.IntegrationTests.Api;

[Collection("Downloads")]
public sealed class DownloadQueueTests(FunkArrFixture fixture)
{
    private readonly FunkArrFixture _fixture = fixture;

    [Fact]
    public async Task GetQueue_WithActiveAndQueuedItems_ReturnsTypedResponse()
    {
        var processing = TestData.ProcessingItem();
        var queued = TestData.QueueItem("Tagesschau.2024-01-15");

        var task = _fixture.Client.GetAsync("/api/downloads/queue");

        var probe = _fixture.GetProbe<IDownloadManager>();
        var msg = probe.ExpectMsg<QueryQueue>();
        probe.Reply(new QueueResult(
            [processing, queued],
            TotalSlots: 3,
            TotalItems: 2,
            IsPaused: false,
            IsScheduleActive: false,
            NextWindow: null));

        var response = await task;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<DownloadQueueResponse>();
        Assert.NotNull(result);

        Assert.Equal(2, result.Items.Length);
        Assert.Equal(3, result.TotalSlots);
        Assert.Equal(1, result.ActiveCount);
        Assert.Equal(1, result.QueuedCount);
        Assert.False(result.IsPaused);
        Assert.False(result.IsScheduleActive);

        var first = result.Items[0];
        Assert.Equal(processing.DownloadId.ToString(), first.DownloadId);
        Assert.Equal(processing.Title, first.Title);
        Assert.Equal(QueueStatus.Processing, first.Status);

        var second = result.Items[1];
        Assert.Equal(queued.DownloadId.ToString(), second.DownloadId);
        Assert.Equal(queued.Title, second.Title);
        Assert.Equal(QueueStatus.Queued, second.Status);
    }

    [Fact]
    public async Task GetQueue_Empty_ReturnsEmptyItems()
    {
        var task = _fixture.Client.GetAsync("/api/downloads/queue");

        var probe = _fixture.GetProbe<IDownloadManager>();
        probe.ExpectMsg<QueryQueue>();
        probe.Reply(new QueueResult(
            [],
            TotalSlots: 3,
            TotalItems: 0,
            IsPaused: false,
            IsScheduleActive: false,
            NextWindow: null));

        var response = await task;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<DownloadQueueResponse>();
        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.ActiveCount);
        Assert.Equal(0, result.QueuedCount);
    }
}
