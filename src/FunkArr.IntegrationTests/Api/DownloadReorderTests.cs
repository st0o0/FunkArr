using System.Net;
using System.Net.Http.Json;
using FunkArr.Api.Models;
using FunkArr.Core;
using FunkArr.Messages.Download;

namespace FunkArr.IntegrationTests.Api;

[Collection("Downloads")]
public sealed class DownloadReorderTests(FunkArrFixture fixture)
{
    private readonly FunkArrFixture _fixture = fixture;

    [Fact]
    public async Task Move_Success_ReturnsOk()
    {
        var id = Guid.NewGuid();

        var task = _fixture.Client.PostAsJsonAsync(
            $"/api/downloads/queue/{id}/move",
            new { position = 0 });

        var probe = _fixture.GetProbe<IDownloadManager>();
        var msg = probe.ExpectMsg<MoveDownload>();
        Assert.Equal(id, msg.DownloadId);
        Assert.Equal(0, msg.Position);
        probe.Reply(new MoveDownloadCompleted());

        var response = await task;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<OperationResult>();
        Assert.NotNull(result);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task Move_InvalidPriority_Returns400WithoutProbeInteraction()
    {
        var id = Guid.NewGuid();

        var response = await _fixture.Client.PostAsJsonAsync(
            $"/api/downloads/queue/{id}/move",
            new { position = 0, priority = "Invalid" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<OperationResult>();
        Assert.NotNull(result);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task Priority_Success_ReturnsOk()
    {
        var id = Guid.NewGuid();

        var task = _fixture.Client.PostAsJsonAsync(
            $"/api/downloads/queue/{id}/priority",
            new { priority = "High" });

        var probe = _fixture.GetProbe<IDownloadManager>();
        var msg = probe.ExpectMsg<SetDownloadPriority>();
        Assert.Equal(id, msg.DownloadId);
        Assert.Equal(DownloadPriority.High, msg.Priority);
        probe.Reply(new SetDownloadPriorityCompleted());

        var response = await task;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<OperationResult>();
        Assert.NotNull(result);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task Priority_Invalid_Returns400()
    {
        var id = Guid.NewGuid();

        var response = await _fixture.Client.PostAsJsonAsync(
            $"/api/downloads/queue/{id}/priority",
            new { priority = "Invalid" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<OperationResult>();
        Assert.NotNull(result);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task Swap_Success_ReturnsOk()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        var task = _fixture.Client.PostAsJsonAsync(
            "/api/downloads/queue/swap",
            new { id1, id2 });

        var probe = _fixture.GetProbe<IDownloadManager>();
        var msg = probe.ExpectMsg<SwapDownloads>();
        Assert.Equal(id1, msg.DownloadId1);
        Assert.Equal(id2, msg.DownloadId2);
        probe.Reply(new SwapDownloadsCompleted());

        var response = await task;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<OperationResult>();
        Assert.NotNull(result);
        Assert.True(result.Success);
    }
}
