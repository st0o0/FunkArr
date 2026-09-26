using System.Net;
using System.Net.Http.Json;
using FunkArr.Api.Models;
using FunkArr.Core;
using FunkArr.Messages.Download;

namespace FunkArr.Api.Tests.Integration;

public sealed class DownloadApiIntegrationTests : IAsyncLifetime
{
    private FunkArrTestServer _server = null!;

    public async ValueTask InitializeAsync() => _server = await FunkArrTestServer.CreateAsync();
    public async ValueTask DisposeAsync() => await _server.DisposeAsync();

    [Fact]
    public async Task GetQueue_returns_empty_list()
    {
        var probe = _server.GetProbe<IDownloadManager>();
        var task = _server.Client.GetAsync("/api/downloads/queue");

        var msg = probe.ExpectMsg<QueryQueue>();
        probe.Reply(new QueueResult([], 3, 0, false, false, null));

        var response = await task;
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"items\"", body);
    }

    [Fact]
    public async Task GetHistory_returns_empty_list()
    {
        var probe = _server.GetProbe<IDownloadHistoryManager>();
        var task = _server.Client.GetAsync("/api/downloads/history");

        probe.ExpectMsg<QueryHistory>();
        probe.Reply(new HistoryResult([], 0));

        var response = await task;
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PostPause_returns_success()
    {
        var probe = _server.GetProbe<IDownloadManager>();
        var task = _server.Client.PostAsync("/api/downloads/pause", null);

        probe.ExpectMsg<PauseDownloads>();
        probe.Reply(new PauseDownloadsResult(true));

        var response = await task;
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<OperationResult>();
        Assert.True(result!.Success);
    }

    [Fact]
    public async Task PostResume_returns_success()
    {
        var probe = _server.GetProbe<IDownloadManager>();
        var task = _server.Client.PostAsync("/api/downloads/resume", null);

        probe.ExpectMsg<ResumeDownloads>();
        probe.Reply(new ResumeDownloadsResult(true));

        var response = await task;
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<OperationResult>();
        Assert.True(result!.Success);
    }
}
