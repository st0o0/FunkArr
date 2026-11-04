using System.Net;
using System.Net.Http.Json;
using FunkArr.Api.Models.Shared;
using FunkArr.Core.Actors;
using FunkArr.Messages.Download;

namespace FunkArr.IntegrationTests.Api;

[Collection("Downloads")]
public sealed class DownloadReorderTests(FunkArrFixture fixture)
{
    [Fact]
    public async Task Move_Success_ReturnsOk()
    {
        var id = Guid.NewGuid();

        var task = fixture.Client.PostAsJsonAsync($"/api/downloads/queue/{id}/move", new { position = 0 }, cancellationToken: TestContext.Current.CancellationToken);

        var probe = fixture.GetProbe<IDownloadManager>();
        var msg = probe.ExpectMsg<MoveDownload>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(id, msg.DownloadId);
        Assert.Equal(0, msg.Position);
        probe.Reply(new MoveDownloadCompleted());

        var response = await task;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<OperationResult>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task Move_InvalidPriority_Returns400WithoutProbeInteraction()
    {
        var id = Guid.NewGuid();

        var response = await fixture.Client.PostAsJsonAsync($"/api/downloads/queue/{id}/move", new { position = 0, priority = "Invalid" }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var probe = fixture.GetProbe<IDownloadManager>();
        probe.ExpectNoMsg(TimeSpan.FromMilliseconds(300), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Priority_Success_ReturnsOk()
    {
        var id = Guid.NewGuid();

        var task = fixture.Client.PostAsJsonAsync($"/api/downloads/queue/{id}/priority", new { priority = 1 }, cancellationToken: TestContext.Current.CancellationToken);

        var probe = fixture.GetProbe<IDownloadManager>();
        var msg = probe.ExpectMsg<SetDownloadPriority>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(id, msg.DownloadId);
        Assert.Equal(DownloadPriority.High, msg.Priority);
        probe.Reply(new SetDownloadPriorityCompleted());

        var response = await task;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<OperationResult>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task Priority_Invalid_Returns400()
    {
        var id = Guid.NewGuid();

        var response = await fixture.Client.PostAsJsonAsync($"/api/downloads/queue/{id}/priority", new { priority = "Invalid" }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var probe = fixture.GetProbe<IDownloadManager>();
        probe.ExpectNoMsg(TimeSpan.FromMilliseconds(300), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Swap_Success_ReturnsOk()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        var task = fixture.Client.PostAsJsonAsync("/api/downloads/queue/swap", new { id1, id2 }, cancellationToken: TestContext.Current.CancellationToken);

        var probe = fixture.GetProbe<IDownloadManager>();
        var msg = probe.ExpectMsg<SwapDownloads>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(id1, msg.DownloadId1);
        Assert.Equal(id2, msg.DownloadId2);
        probe.Reply(new SwapDownloadsCompleted());

        var response = await task;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<OperationResult>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.True(result.Success);
    }
}
