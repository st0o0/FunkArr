using System.Net;
using System.Net.Http.Json;
using FunkArr.Api.Models;
using FunkArr.Core;
using FunkArr.Messages.Download;

namespace FunkArr.IntegrationTests.Api;

[Collection("Downloads")]
public sealed class DownloadHistoryTests(FunkArrFixture fixture)
{
    private readonly FunkArrFixture _fixture = fixture;

    [Fact]
    public async Task GetHistory_WithEntries_ReturnsTypedResponse()
    {
        var completed = TestData.CompletedItem();
        var failed = TestData.FailedItem();

        var task = _fixture.Client.GetAsync("/api/downloads/history");

        var probe = _fixture.GetProbe<IDownloadHistoryManager>();
        probe.ExpectMsg<QueryHistory>();
        probe.Reply(new HistoryResult([completed, failed], TotalItems: 2));

        var response = await task;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<DownloadHistoryResponse>();
        Assert.NotNull(result);

        Assert.Equal(2, result.Items.Length);
        Assert.Equal(2, result.TotalItems);

        var first = result.Items[0];
        Assert.Equal(completed.DownloadId.ToString(), first.DownloadId);
        Assert.Equal(completed.Completion.Title, first.Title);
        Assert.Equal(HistoryStatus.Completed, first.Status);

        var second = result.Items[1];
        Assert.Equal(failed.DownloadId.ToString(), second.DownloadId);
        Assert.Equal(failed.Completion.Title, second.Title);
        Assert.Equal(HistoryStatus.Failed, second.Status);
        Assert.Equal(failed.Completion.FailMessage, second.FailMessage);
    }

    [Fact]
    public async Task GetHistoryStats_ReturnsTypedResponse()
    {
        var task = _fixture.Client.GetAsync("/api/downloads/history/stats");

        var probe = _fixture.GetProbe<IDownloadHistoryManager>();
        probe.ExpectMsg<QueryHistoryStats>();
        probe.Reply(new HistoryStatsResult(
            TotalCompleted: 42,
            TotalFailed: 3,
            TotalBytes: 50_000_000_000,
            AverageDownloadTimeSeconds: 95,
            SuccessRate: 0.933));

        var response = await task;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<HistoryStatsResponse>();
        Assert.NotNull(result);
        Assert.Equal(42, result.TotalCompleted);
        Assert.Equal(3, result.TotalFailed);
        Assert.Equal(50_000_000_000, result.TotalBytes);
        Assert.Equal(95, result.AverageDownloadTimeSeconds);
        Assert.Equal(0.933, result.SuccessRate);
    }

    [Fact]
    public async Task GetHistoryCategories_ReturnsArray()
    {
        var task = _fixture.Client.GetAsync("/api/downloads/history/categories");

        var probe = _fixture.GetProbe<IDownloadHistoryManager>();
        probe.ExpectMsg<QueryHistoryCategories>();
        probe.Reply(new HistoryCategoriesResult(["tv", "movie"]));

        var response = await task;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<string[]>();
        Assert.NotNull(result);
        Assert.Equal(2, result.Length);
        Assert.Contains("tv", result);
        Assert.Contains("movie", result);
    }
}
