using System.Net;
using System.Net.Http.Json;
using FunkArr.Api.Models;
using FunkArr.Core;
using FunkArr.Messages.Feed;
using FunkArr.Messages.Search;
using FunkArr.Messages.Shared;

namespace FunkArr.IntegrationTests.Api;

[Collection("Feed")]
public sealed class FeedTests(FunkArrFixture fixture)
{
    private readonly FunkArrFixture _fixture = fixture;

    [Fact]
    public async Task Feed_overview_returns_entries_and_ingest_status()
    {
        var feedProbe = _fixture.GetProbe<IFeedManager>();
        var ingestProbe = _fixture.GetProbe<IFeedIngestManager>();
        var lastRun = new DateTimeOffset(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);
        var entry = new FeedEntry("0123456789abcdef0123456789abcdef", lastRun, FunkArr.Messages.MediaType.Show,
            new SearchResultItem("Tatort.S2026E18.GERMAN.720p.WEB.h264-FunkArr", "ARD", "Tatort",
                "https://example.com/v.mp4", 5400, 1_000_000, 720, null, 0.9,
                Metadata: new MatchMetadata(null, "2026", "18", null, null)));

        var task = _fixture.Client.GetAsync("/api/feed?limit=10");

        var query = feedProbe.ExpectMsg<QueryFeed>();
        Assert.Equal(10, query.Limit);
        feedProbe.Reply(new FeedResult([entry], 1));
        ingestProbe.ExpectMsg<QueryFeedIngestStatus>();
        ingestProbe.Reply(new FeedIngestStatusResult(true, false, 1_761_800_000, lastRun, "completed", 12, 1, 3, false, 0));

        var response = await task;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<FeedOverviewResponse>();
        Assert.NotNull(result);
        var output = Assert.Single(result.Entries);
        Assert.Multiple(
            () => Assert.Equal(1, result.Total),
            () => Assert.Equal("0123456789abcdef0123456789abcdef", output.StableId),
            () => Assert.Equal("18", output.Episode),
            () => Assert.Equal("completed", result.Ingest.LastOutcome),
            () => Assert.Equal(3, result.Ingest.LastUnmatched),
            () => Assert.Equal(12, result.Ingest.LastNewItems));
    }

    [Fact]
    public async Task Crawl_returns_accepted_and_triggers_ingest()
    {
        var ingestProbe = _fixture.GetProbe<IFeedIngestManager>();

        var response = await _fixture.Client.PostAsync("/api/feed/crawl", null);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        ingestProbe.ExpectMsg<TriggerFeedCrawl>();
    }
}
