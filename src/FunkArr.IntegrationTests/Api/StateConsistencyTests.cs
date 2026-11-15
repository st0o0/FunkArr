using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FunkArr.Api.Models.Downloads;
using FunkArr.Core.Actors;
using FunkArr.Messages.Download;

namespace FunkArr.IntegrationTests.Api;

[Collection("Downloads")]
public sealed class StateConsistencyTests(FunkArrFixture fixture)
{
    [Fact]
    public async Task DownloadQueue_InternalApi_and_SabnzbdApi_return_same_count()
    {
        var items = new[]
        {
            TestData.ProcessingItem("Show.S01E01"),
            TestData.QueueItem("Show.S01E02"),
            TestData.QueueItem("Show.S01E03")
        };
        var queueResult = new QueueResult(items, 2, 3, false, false, null);

        var internalTask = fixture.Client.GetAsync("/api/downloads/queue", TestContext.Current.CancellationToken);
        var probe = fixture.GetProbe<IDownloadManager>();
        probe.ExpectMsg<QueryQueue>(cancellationToken: TestContext.Current.CancellationToken);
        probe.Reply(queueResult);
        var internalResponse = await internalTask;

        Assert.Equal(HttpStatusCode.OK, internalResponse.StatusCode);
        var internalResult = await internalResponse.Content.ReadFromJsonAsync<DownloadQueueResponse>(
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(internalResult);

        var sabnzbdTask = fixture.Client.GetAsync("/download/api?mode=queue&apikey=test-key",
            TestContext.Current.CancellationToken);
        probe.ExpectMsg<QueryQueue>(cancellationToken: TestContext.Current.CancellationToken);
        probe.Reply(queueResult);
        var sabnzbdResponse = await sabnzbdTask;

        Assert.Equal(HttpStatusCode.OK, sabnzbdResponse.StatusCode);
        var sabnzbdJson = await sabnzbdResponse.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: TestContext.Current.CancellationToken);
        var slots = sabnzbdJson.GetProperty("queue").GetProperty("slots");

        Assert.Equal(internalResult.Items.Length, slots.GetArrayLength());
    }
}
