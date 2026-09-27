using System.Net;
using System.Net.Http.Json;
using FunkArr.Api.Models;
using FunkArr.Core;
using FunkArr.Messages.Download;

namespace FunkArr.IntegrationTests.Api;

[Collection("Downloads")]
public sealed class DownloadMutationTests(FunkArrFixture fixture)
{
    private readonly FunkArrFixture _fixture = fixture;

    [Fact]
    public async Task Cancel_Success_ReturnsOk()
    {
        var id = Guid.NewGuid();

        var task = _fixture.Client.DeleteAsync($"/api/downloads/queue/{id}");

        var probe = _fixture.GetProbe<IDownloadManager>();
        var msg = probe.ExpectMsg<DeleteDownload>();
        Assert.Equal(id, msg.DownloadId);
        probe.Reply(new DeleteDownloadResult(true, null));

        var response = await task;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<OperationResult>();
        Assert.NotNull(result);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task Cancel_NotFound_Returns404()
    {
        var id = Guid.NewGuid();

        var task = _fixture.Client.DeleteAsync($"/api/downloads/queue/{id}");

        var probe = _fixture.GetProbe<IDownloadManager>();
        probe.ExpectMsg<DeleteDownload>();
        probe.Reply(new DeleteDownloadResult(false, "Not found"));

        var response = await task;

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<OperationResult>();
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal("Not found", result.Error);
    }

    [Fact]
    public async Task Retry_Success_ReturnsOk()
    {
        var id = Guid.NewGuid();

        var task = _fixture.Client.PostAsync($"/api/downloads/{id}/retry", null);

        // The endpoint does history.Tell(RemoveHistoryEntry) first, then manager.Ask(RetryDownload)
        var historyProbe = _fixture.GetProbe<IDownloadHistoryManager>();
        var historyMsg = historyProbe.ExpectMsg<RemoveHistoryEntry>();
        Assert.Equal(id, historyMsg.DownloadId);

        var managerProbe = _fixture.GetProbe<IDownloadManager>();
        var managerMsg = managerProbe.ExpectMsg<RetryDownload>();
        Assert.Equal(id, managerMsg.DownloadId);
        managerProbe.Reply(new RetryDownloadResult(true, null));

        var response = await task;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<OperationResult>();
        Assert.NotNull(result);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task Retry_FailedState_Returns400()
    {
        var id = Guid.NewGuid();

        var task = _fixture.Client.PostAsync($"/api/downloads/{id}/retry", null);

        var historyProbe = _fixture.GetProbe<IDownloadHistoryManager>();
        historyProbe.ExpectMsg<RemoveHistoryEntry>();

        var managerProbe = _fixture.GetProbe<IDownloadManager>();
        managerProbe.ExpectMsg<RetryDownload>();
        managerProbe.Reply(new RetryDownloadResult(false, "Not failed"));

        var response = await task;

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<OperationResult>();
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal("Not failed", result.Error);
    }

    [Fact]
    public async Task ForceStart_Success_ReturnsOk()
    {
        var id = Guid.NewGuid();

        var task = _fixture.Client.PostAsync($"/api/downloads/queue/{id}/force-start", null);

        var probe = _fixture.GetProbe<IDownloadManager>();
        var msg = probe.ExpectMsg<ForceStartDownload>();
        Assert.Equal(id, msg.DownloadId);
        probe.Reply(new ForceStartDownloadResult(true, null));

        var response = await task;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<OperationResult>();
        Assert.NotNull(result);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task ForceStart_NotQueued_Returns400()
    {
        var id = Guid.NewGuid();

        var task = _fixture.Client.PostAsync($"/api/downloads/queue/{id}/force-start", null);

        var probe = _fixture.GetProbe<IDownloadManager>();
        probe.ExpectMsg<ForceStartDownload>();
        probe.Reply(new ForceStartDownloadResult(false, "Not queued"));

        var response = await task;

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<OperationResult>();
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal("Not queued", result.Error);
    }
}
