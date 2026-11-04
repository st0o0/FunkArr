using System.Net;
using System.Net.Http.Json;
using FunkArr.Api.Models;
using FunkArr.Core;
using FunkArr.Messages.Download;

namespace FunkArr.IntegrationTests.Api;

[Collection("Downloads")]
public sealed class DownloadPipelineTests(FunkArrFixture fixture)
{
    private readonly FunkArrFixture _fixture = fixture;

    [Fact]
    public async Task Pause_ReturnsOperationResult()
    {
        var task = _fixture.Client.PostAsync("/api/downloads/pause", null, TestContext.Current.CancellationToken);

        var probe = _fixture.GetProbe<IDownloadManager>();
        probe.ExpectMsg<PauseDownloads>(cancellationToken: TestContext.Current.CancellationToken);
        probe.Reply(new PauseDownloadsResult(true));

        var response = await task;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<OperationResult>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task Resume_ReturnsOperationResult()
    {
        var task = _fixture.Client.PostAsync("/api/downloads/resume", null, TestContext.Current.CancellationToken);

        var probe = _fixture.GetProbe<IDownloadManager>();
        probe.ExpectMsg<ResumeDownloads>(cancellationToken: TestContext.Current.CancellationToken);
        probe.Reply(new ResumeDownloadsResult(true));

        var response = await task;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<OperationResult>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.True(result.Success);
    }
}
